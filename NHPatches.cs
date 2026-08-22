using HarmonyLib;
using NewHorizons;
using NewHorizons.Builder.Props;
using NewHorizons.Builder.Props.TranslatorText;
using NewHorizons.External;
using NewHorizons.External.Modules.Props;
using NewHorizons.External.Modules.TranslatorText;
using OWML.Common;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LingeringTranslations;

public static class NHPatches
{
    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(TranslatorTextBuilder),
        nameof(TranslatorTextBuilder.Make),
        [
            typeof(GameObject),
            typeof(Sector),
            typeof(TranslatorTextInfo),
            typeof(NewHorizonsBody),
            typeof(string)
        ]
    )]
    public static void TranslatorTextMakePostfix(
        TranslatorTextInfo info,
        NewHorizonsBody nhBody,
        GameObject __result)
    {
        if (__result == null ||
            info == null ||
            nhBody?.Mod == null ||
            string.IsNullOrEmpty(info.xmlFile))
        {
            return;
        }

        foreach (var nomaiText in __result.GetComponentsInChildren<NomaiText>(true))
        {
            TranslationUtils.RegisterTranslationSource(
                nomaiText,
                nhBody.Mod.ModHelper.Manifest.UniqueName,
                info.xmlFile
            );
        }
    }

    [HarmonyPostfix]
#pragma warning disable CS0612
    [HarmonyPatch(typeof(NomaiTextBuilder), nameof(NomaiTextBuilder.Make))]
#pragma warning restore CS0612
    public static void NomaiTextMakePostfix(
        NomaiTextInfo info,
        IModBehaviour mod,
        GameObject __result)
    {
        if (__result == null ||
            info == null ||
            mod == null ||
            string.IsNullOrEmpty(info.xmlFile))
        {
            return;
        }

        foreach (var nomaiText in __result.GetComponentsInChildren<NomaiText>(true))
        {
            TranslationUtils.RegisterTranslationSource(
                nomaiText,
                mod.ModHelper.Manifest.UniqueName,
                info.xmlFile
            );
        }
    }

    [HarmonyPatch]
    public static class DetailBuilderPatches
    {
        [System.ThreadStatic]
        private static Stack<IModBehaviour> _modStack;

        public static IModBehaviour CurrentMod =>
            _modStack != null && _modStack.Count > 0
                ? _modStack.Peek()
                : null;

        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                typeof(DetailBuilder),
                nameof(DetailBuilder.Make),
                [
                    typeof(GameObject),
                    typeof(Sector).MakeByRefType(),
                    typeof(IModBehaviour),
                    typeof(GameObject),
                    typeof(DetailInfo)
                ]
            );
        }

        [HarmonyPrefix]
        public static void Prefix(IModBehaviour mod)
        {
            _modStack ??= new Stack<IModBehaviour>();
            _modStack.Push(mod);
        }

        [HarmonyFinalizer]
        public static void Finalizer()
        {
            if (_modStack == null || _modStack.Count == 0)
            {
                return;
            }

            _modStack.Pop();
        }
    }

    // Called in DetailBuilder.Make
    [HarmonyPostfix]
    [HarmonyPatch(
        typeof(TranslatorTextBuilder),
        nameof(TranslatorTextBuilder.HandleUnityCreatedNomaiText)
    )]
    public static void HandleUnityCreatedNomaiTextPostfix(NomaiText nomaiText)
    {
        if (nomaiText == null)
        {
            return;
        }

        var mod = DetailBuilderPatches.CurrentMod;

        if (mod == null)
        {
            Main.Instance.ModHelper.Console.WriteLine(
                $"HandleUnityCreatedNomaiText called without DetailBuilder mod context for [{nomaiText.name}]",
                MessageType.Warning
            );

            return;
        }

        string assetName = nomaiText._nomaiTextAsset != null
            ? nomaiText._nomaiTextAsset.name
            : nomaiText.name;

        TranslationUtils.RegisterTranslationSource(
            nomaiText,
            mod.ModHelper.Manifest.UniqueName,
            $"unity:{assetName}"
        );
    }
}