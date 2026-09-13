using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

// Minimal behavioral fixtures, not the game's implementation. No game assets are included.
public class NomaiText
{
    public enum Location { Unspecified, A, B }
    public Location _location;
    public readonly Dictionary<int, bool> Translated = new();
    public readonly List<(Location Location, int[] Blocks, string Fact)> Conditions = new();
    public readonly HashSet<string> Facts = new();
    public int Checks;
    public bool ThrowOnCheck;

    public bool IsTranslated(int id) => Translated.TryGetValue(id, out bool value) && value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual void SetAsTranslated(int id)
    {
        if (!Translated.ContainsKey(id) || IsTranslated(id)) return;
        Translated[id] = true;
        CheckSetDatabaseCondition();
    }

    public void CheckSetDatabaseCondition()
    {
        if (ThrowOnCheck) throw new InvalidOperationException("Test discovery failure");
        Checks++;
        foreach (var condition in Conditions)
        {
            if ((condition.Location == Location.Unspecified || condition.Location == _location)
                && condition.Blocks.All(IsTranslated))
            {
                Facts.Add(condition.Fact);
            }
        }
    }
}

public class DerivedNomaiText : NomaiText
{
    public int VisualUpdates;
    public override void SetAsTranslated(int id)
    {
        base.SetAsTranslated(id);
        VisualUpdates++;
    }
}

public class NomaiTranslatorProp
{
    public NomaiText Text;
    public int Id = 1;
    public bool Complete = true;

    public void Display() => DisplayTextNode();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void DisplayTextNode()
    {
        if (Complete) Text.SetAsTranslated(Id);
    }
}

namespace LingeringTranslations
{
    public static class TranslationUtils
    {
        public static bool IsSilentlyRestoring;
    }
}
