using System;
using System.Collections.Generic;
using OWML.Common;

namespace LingeringTranslations;

// Based on New Horizons's Save
public static class LingeringTranslationsData
{
    private const string FileName = "save.json";

    private static LingeringTranslationsSaveFile _saveFile;
    private static LingeringTranslationsProfile _activeProfile;
    private static string _activeProfileName;

    private static readonly object _lock = new();

    // Separate method in case compatibility mods ever need to patch this.
    public static string GetProfileName()
    {
        return StandaloneProfileManager.SharedInstance?.currentProfile?.profileName;
    }

    public static void Load()
    {
        lock (_lock)
        {
            _activeProfileName = GetProfileName();

            if (_activeProfileName == null)
            {
                LingeringTranslations.Instance.ModHelper.Console.WriteLine(
                    "Couldn't find active profile, using Gamepass fallback.",
                    MessageType.Warning
                );

                _activeProfileName = "XboxGamepassDefaultProfile";
            }

            try
            {
                _saveFile = LingeringTranslations.Instance.ModHelper.Storage.Load<LingeringTranslationsSaveFile>(
                    FileName,
                    false
                );

                if (!_saveFile.Profiles.ContainsKey(_activeProfileName))
                {
                    _saveFile.Profiles.Add(
                        _activeProfileName,
                        new LingeringTranslationsProfile()
                    );
                }

                _activeProfile = _saveFile.Profiles[_activeProfileName];
            }
            catch (Exception)
            {
                try
                {
                    _saveFile = new LingeringTranslationsSaveFile();

                    _saveFile.Profiles.Add(
                        _activeProfileName,
                        new LingeringTranslationsProfile()
                    );

                    _activeProfile = _saveFile.Profiles[_activeProfileName];

                    Save();
                }
                catch (Exception e)
                {
                    LingeringTranslations.Instance.ModHelper.Console.WriteLine(
                        $"Couldn't create save data:\n{e}",
                        MessageType.Error
                    );
                }
            }
        }
    }

    public static void Save()
    {
        if (_saveFile == null)
        {
            return;
        }

        lock (_lock)
        {
            try
            {
                LingeringTranslations.Instance.ModHelper.Storage.Save(_saveFile, FileName);
            }
            catch (Exception e)
            {
                LingeringTranslations.Instance.ModHelper.Console.WriteLine(
                    $"Couldn't save data:\n{e}",
                    MessageType.Error
                );
            }
        }
    }

    public static void Reset()
    {
        if (_saveFile == null || _activeProfile == null)
        {
            Load();
        }

        _activeProfile = new LingeringTranslationsProfile();
        _saveFile.Profiles[_activeProfileName] = _activeProfile;

        Save();
    }

    #region Translations

    public static bool IsTranslated(string sourceId, int id)
    {
        if (_activeProfile == null || string.IsNullOrEmpty(sourceId))
        {
            return false;
        }

        return _activeProfile.NomaiTranslatedEntries.TryGetValue(
                   sourceId,
                   out var ids
               )
               && ids.Contains(id);
    }

    public static void SetTranslated(string sourceId, int id)
    {
        if (_activeProfile == null || string.IsNullOrEmpty(sourceId))
        {
            return;
        }

        if (!_activeProfile.NomaiTranslatedEntries.TryGetValue(
                sourceId,
                out var ids
            ))
        {
            ids = new List<int>();
            _activeProfile.NomaiTranslatedEntries[sourceId] = ids;
        }

        if (ids.Contains(id))
        {
            return;
        }

        ids.Add(id);
        Save();
    }

    public static List<int> GetTranslatedEntries(string sourceId)
    {
        if (_activeProfile == null || string.IsNullOrEmpty(sourceId))
        {
            return new List<int>();
        }

        if (_activeProfile.NomaiTranslatedEntries.TryGetValue(
                sourceId,
                out var ids
            ))
        {
            return ids;
        }

        return new List<int>();
    }

    public static bool TryGetTranslatedEntries(string sourceId, out List<int> ids)
    {
        if (_activeProfile == null || string.IsNullOrEmpty(sourceId))
        {
            ids = null;
            return false;
        }

        return _activeProfile.NomaiTranslatedEntries.TryGetValue(sourceId, out ids);
    }

    #endregion

    private class LingeringTranslationsSaveFile
    {
        public LingeringTranslationsSaveFile()
        {
            Profiles = new Dictionary<string, LingeringTranslationsProfile>();
        }

        public Dictionary<string, LingeringTranslationsProfile> Profiles { get; }
    }

    private class LingeringTranslationsProfile
    {
        public LingeringTranslationsProfile()
        {
            NomaiTranslatedEntries = new Dictionary<string, List<int>>();
        }

        public Dictionary<string, List<int>> NomaiTranslatedEntries { get; }
    }
}