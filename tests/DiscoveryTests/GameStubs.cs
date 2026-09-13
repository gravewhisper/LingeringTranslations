using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// Behavioral fixtures, not the game's implementation. No game assets are included.
public class NomaiText
{
    public struct NomaiTextData { public bool IsTranslated; }
    public enum Location { Unspecified, A, B }
    public Location _location;
    public readonly Dictionary<int, NomaiTextData> _dictNomaiTextData = new();
    public readonly List<(Location Location, int[] Blocks, string Fact)> Conditions = new();
    public readonly HashSet<string> Facts = new();
    public int Checks;
    public int Notifications;
    public int Stores;

    public void SetState(int id, bool translated) =>
        _dictNomaiTextData[id] = new NomaiTextData { IsTranslated = translated };

    public bool IsTranslated(int id) =>
        _dictNomaiTextData.TryGetValue(id, out var data) && data.IsTranslated;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual void SetAsTranslated(int id)
    {
        if (!_dictNomaiTextData.ContainsKey(id) || IsTranslated(id)) return;
        SetState(id, true);
        CheckSetDatabaseCondition();
    }

    public void CheckSetDatabaseCondition()
    {
        Checks++;
        foreach (var condition in Conditions)
        {
            if ((condition.Location == Location.Unspecified || condition.Location == _location)
                && condition.Blocks.All(IsTranslated) && Facts.Add(condition.Fact))
            {
                Notifications++;
            }
        }
    }

    public void LoadTextXml() { }
}

// Compile-only stand-ins for unrelated methods in the linked production Patches.cs.
public class NomaiWallText : NomaiText { }
public static class PlayerData
{
    public static void ResetGame() { }
    public static void SaveCurrentGame() { }
}
public class PlayerAudioController
{
    public void PlayNomaiTextReveal(NomaiWallText text) { }
}
public class NomaiComputerRing
{
    public bool _activated, _translated;
    public float _emissionColorT;
    public Renderer _renderer;
    public static readonly PropertyBlock s_matPropBlock = new();
    public static int s_propID_Detail1EmissionColor, s_colorTranslated;
}
public class Renderer
{
    public void GetPropertyBlock(PropertyBlock block) { }
    public void SetPropertyBlock(PropertyBlock block) { }
}
public class PropertyBlock { public void SetColor(int property, int color) { } }

namespace LingeringTranslations
{
    public static class TranslationUtils
    {
        public static bool IsSilentlyRestoring;
        public static void StoreTranslation(NomaiText text, int id) => text.Stores++;
        public static void RestoreTranslations(NomaiText text) { }
        public static void RegisterSync(NomaiText text) { }
        public static void UnregisterSync(NomaiText text) { }
    }
    public static class LingeringTranslationsData
    {
        public static void Reset() { }
        public static void Save() { }
    }
}
