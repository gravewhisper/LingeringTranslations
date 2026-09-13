using OWML.Common;
using System.Collections.Generic;
using System.Linq;

namespace LingeringTranslations;

public static class TranslationUtils
{
    private static readonly Dictionary<string, HashSet<NomaiText>> _texts = new();

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
    /// Permanently remembers a translation and updates other loaded copies
    /// of the same Nomai text.
    /// </summary>
    public static void StoreTranslation(NomaiText nomaiText, int id)
    {
        if (nomaiText == null ||
            nomaiText._dictNomaiTextData == null ||
            !nomaiText._dictNomaiTextData.ContainsKey(id))
        {
            return;
        }

        string key = GetTranslationKey(nomaiText);

        LingeringTranslations.Instance.ModHelper.Console.WriteLine(
            $"Remembering translation: {key} [{id}]",
            MessageType.Info
        );

        LingeringTranslationsData.SetTranslated(key, id);

        OnTranslationUpdated(key, id);
    }

    /// <summary>
    /// Restores all remembered translations for this Nomai text.
    /// </summary>
    public static void RestoreTranslations(this NomaiText nomaiText)
    {
        if (nomaiText == null)
        {
            return;
        }

        string key = GetTranslationKey(nomaiText);

        if (!LingeringTranslationsData.TryGetTranslatedEntries(
                key,
                out List<int> translatedEntries))
        {
            return;
        }

        LingeringTranslations.Instance.ModHelper.Console.WriteLine(
            $"Restoring {translatedEntries.Count} translations for {key}",
            MessageType.Info
        );

        foreach (int id in translatedEntries)
        {
            RestoreTranslation(nomaiText, id, key);
        }
    }

    private static void RestoreTranslation(
        NomaiText nomaiText,
        int id)
    {
        RestoreTranslation(nomaiText, id, GetTranslationKey(nomaiText));
    }

    private static void RestoreTranslation(
        NomaiText nomaiText,
        int id,
        string key)
    {
        if (nomaiText == null ||
            nomaiText._dictNomaiTextData == null ||
            !nomaiText._dictNomaiTextData.TryGetValue(id, out var data))
        {
            LingeringTranslations.Instance.ModHelper.Console.WriteLine(
                $"Could not restore {key} [{id}]: entry doesn't exist",
                MessageType.Warning
            );

            return;
        }

        if (!data.IsTranslated)
        {
            // A newly remembered block can satisfy conditions that a previous read could not.
            TranslationDiscoveryPatch.Forget(nomaiText);
        }

        data.IsTranslated = true;
        nomaiText._dictNomaiTextData[id] = data;

        LingeringTranslations.Instance.ModHelper.Events.Unity.FireInNUpdates(() =>
        {
            SetAsTranslatedSilently(nomaiText, id);
        }, 2);

        LingeringTranslations.Instance.ModHelper.Console.WriteLine(
            $"Restored translation: {key} [{id}]",
            MessageType.Info
        );
    }

    private static int _silentRestoreDepth;

    public static bool IsSilentlyRestoring => _silentRestoreDepth > 0;

    public static void SetAsTranslatedSilently(NomaiText nomaiText, int id)
    {
        if (nomaiText == null)
        {
            return;
        }

        _silentRestoreDepth++;

        try
        {
            nomaiText.SetAsTranslated(id);
        }
        finally
        {
            _silentRestoreDepth--;
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

        // Important: the key may have just changed from vanilla:* to the
        // explicitly registered mod/source key.
        RegisterSync(nomaiText);

        RestoreTranslations(nomaiText);
    }

    public static void RegisterSync(NomaiText nomaiText)
    {
        if (nomaiText == null)
        {
            return;
        }

        UnregisterSync(nomaiText);

        string key = GetTranslationKey(nomaiText);

        if (!_texts.TryGetValue(key, out var texts))
        {
            texts = new HashSet<NomaiText>();
            _texts[key] = texts;
        }

        texts.Add(nomaiText);
    }

    public static void UnregisterSync(NomaiText nomaiText)
    {
        if (nomaiText == null)
        {
            return;
        }

        foreach (var pair in _texts.ToArray())
        {
            pair.Value.Remove(nomaiText);

            if (pair.Value.Count == 0)
            {
                _texts.Remove(pair.Key);
            }
        }
    }

    private static void OnTranslationUpdated(string sourceId, int id)
    {
        if (!_texts.TryGetValue(sourceId, out var texts))
        {
            return;
        }

        foreach (var nomaiText in texts.ToArray())
        {
            if (nomaiText == null)
            {
                texts.Remove(nomaiText);
                continue;
            }

            // We already know this group uses sourceId, so don't recalculate it.
            RestoreTranslation(nomaiText, id, sourceId);
        }

        if (texts.Count == 0)
        {
            _texts.Remove(sourceId);
        }
    }
}