using HarmonyLib;

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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NomaiText), nameof(NomaiText.SetAsTranslated))]
    public static void SetAsTranslatedPostfix(
        NomaiText __instance,
        int id)
    {
        TranslationUtils.StoreTranslation(__instance, id);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(NomaiText), nameof(NomaiText.LoadTextXml))]
    public static void LoadTextXmlPostfix(NomaiText __instance)
    {
        TranslationUtils.RestoreTranslations(__instance);
    }
}