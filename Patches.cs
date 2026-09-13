using HarmonyLib;
using System.Collections.Generic;

namespace LingeringTranslations;

public static class Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.ResetGame))]
    public static void PlayerData_ResetGame()
    {
        LingeringTranslationsData.Reset();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerData), nameof(PlayerData.SaveCurrentGame))]
    public static void PlayerData_SaveCurrentGame()
    {
        LingeringTranslationsData.Save();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NomaiText), nameof(NomaiText.SetAsTranslated))]
    public static void SetAsTranslatedPrefix(
    NomaiText __instance,
    int id,
    out bool __state)
    {
        __state =
            __instance._dictNomaiTextData != null &&
            __instance._dictNomaiTextData.TryGetValue(id, out var data) &&
            data.IsTranslated;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NomaiText), nameof(NomaiText.SetAsTranslated))]
    public static void SetAsTranslatedPostfix(
        NomaiText __instance,
        int id,
        bool __state)
    {
        // It was already translated before this call.
        if (__state)
        {
            if (!TranslationUtils.IsSilentlyRestoring)
            {
                __instance.CheckSetDatabaseCondition();
            }

            return;
        }

        // Make sure the original method actually translated it.
        if (__instance._dictNomaiTextData == null ||
            !__instance._dictNomaiTextData.TryGetValue(id, out var data) ||
            !data.IsTranslated)
        {
            return;
        }

        TranslationUtils.StoreTranslation(__instance, id);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NomaiText), nameof(NomaiText.LoadTextXml))]
    public static void LoadTextXmlPostfix(NomaiText __instance)
    {
        TranslationUtils.RestoreTranslations(__instance);
        TranslationUtils.RegisterSync(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NomaiText), "OnDestroy")]
    public static void NomaiTextOnDestroyPrefix(NomaiText __instance)
    {
        TranslationUtils.UnregisterSync(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NomaiComputerRing), "Update")]
    public static void NomaiComputerRingUpdatePostfix(NomaiComputerRing __instance)
    {
        if (__instance._activated &&
            __instance._translated &&
            __instance._emissionColorT <= 0f &&
            __instance._renderer != null)
        {
            __instance._renderer.GetPropertyBlock(
                NomaiComputerRing.s_matPropBlock
            );

            NomaiComputerRing.s_matPropBlock.SetColor(
                NomaiComputerRing.s_propID_Detail1EmissionColor,
                NomaiComputerRing.s_colorTranslated
            );

            __instance._renderer.SetPropertyBlock(
                NomaiComputerRing.s_matPropBlock
            );
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(
        typeof(PlayerAudioController),
        nameof(PlayerAudioController.PlayNomaiTextReveal),
        [typeof(NomaiWallText)]
    )]
    public static bool PlayNomaiTextRevealPrefix()
    {
        return !TranslationUtils.IsSilentlyRestoring;
    }
}