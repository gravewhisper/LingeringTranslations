using OWML.Common;
using System.Collections.Generic;
using System.Linq;

namespace LingeringTranslations;

public static class TranslationUtils
{
    public static string GetTranslationKey(NomaiText nomaiText)
    {
        var source = nomaiText.GetComponent<TranslationSource>();

        if (source != null)
        {
            return source.Key;
        }

        string assetName = nomaiText._nomaiTextAsset != null
            ? nomaiText._nomaiTextAsset.name
            : nomaiText.name;

        return $"vanilla:{assetName}";
    }

    /// <summary>
    /// Permanently remembers a translation whenever a Nomai text entry is translated.
    /// </summary>
    public static void StoreTranslation(NomaiText nomaiText, int id)
    {
        if (nomaiText == null)
        {
            return;
        }

        // Don't save invalid IDs.
        if (!nomaiText._dictNomaiTextData.ContainsKey(id))
        {
            return;
        }

        string key = GetTranslationKey(nomaiText);

        LingeringTranslations.Instance.ModHelper.Console.WriteLine(
            $"Remembering translation: {key} [{id}]",
            MessageType.Info
        );

        LingeringTranslationsData.SetTranslated(key, id);
    }

    /// <summary>
    /// Restores remembered translation states after the text XML has been loaded.
    /// </summary>
    public static void RestoreTranslations(NomaiText nomaiText)
    {
        string key = GetTranslationKey(nomaiText);

        var translatedEntries =
            LingeringTranslationsData.GetTranslatedEntries(key);

        if (translatedEntries.Count == 0)
        {
            return;
        }

        LingeringTranslations.Instance.ModHelper.Console.WriteLine(
            $"Restoring {translatedEntries.Count} translations for {key}",
            MessageType.Info
        );

        foreach (int id in translatedEntries)
        {
            if (!nomaiText._dictNomaiTextData.TryGetValue(id, out var data))
            {
                LingeringTranslations.Instance.ModHelper.Console.WriteLine(
                    $"Could not restore {key} [{id}]: entry doesn't exist",
                    MessageType.Warning
                );

                continue;
            }

            data.IsTranslated = true;
            nomaiText._dictNomaiTextData[id] = data;

            LingeringTranslations.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                nomaiText.SetAsTranslated(id);
            });

            LingeringTranslations.Instance.ModHelper.Console.WriteLine(
                $"Restored translation: {key} [{id}]",
                MessageType.Info
            );
        }
    }

    public static void RegisterTranslationSource(
        NomaiText nomaiText,
        string modUniqueName,
        string sourceId)
    {
        if (nomaiText == null ||
            string.IsNullOrEmpty(modUniqueName) ||
            string.IsNullOrEmpty(sourceId))
        {
            return;
        }

        sourceId = sourceId.Replace('\\', '/');

        var source = nomaiText.GetComponent<TranslationSource>();

        if (source == null)
        {
            source = nomaiText.gameObject.AddComponent<TranslationSource>();
        }

        source.ModUniqueName = modUniqueName;
        source.SourceID = sourceId;

        LingeringTranslations.Instance.ModHelper.Console.WriteLine(
            $"Registered translation source: {source.Key}",
            MessageType.Info
        );

        RestoreTranslations(nomaiText);
    }
}