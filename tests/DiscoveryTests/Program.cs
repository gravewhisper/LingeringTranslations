using HarmonyLib;
using LingeringTranslations;
using System;
using Patches = LingeringTranslations.Patches;

internal static class Program
{
    private static int _passed;

    private static NomaiText Text(bool restored = true, NomaiText.Location location = NomaiText.Location.A)
    {
        var text = new NomaiText { _location = location };
        text.SetState(1, restored);
        text.Conditions.Add((location, new[] { 1 }, location.ToString()));
        return text;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void Test(string name, Action action)
    {
        action();
        _passed++;
        Console.WriteLine($"PASS {name}");
    }

    public static void Main()
    {
        Test("baseline reproduced: restored text skips discovery", () =>
        {
            var text = Text();
            text.SetAsTranslated(1);
            Assert(text.Checks == 0 && text.Facts.Count == 0, "Baseline did not reproduce");
        });

        // Apply the actual prefix/postfix from production Patches.cs, not a copy of the fix.
        new Harmony("gravewhisper.LingeringTranslations.Tests").Patch(
            AccessTools.Method(typeof(NomaiText), nameof(NomaiText.SetAsTranslated)),
            prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.SetAsTranslatedPrefix)),
            postfix: new HarmonyMethod(typeof(Patches), nameof(Patches.SetAsTranslatedPostfix)));

        Test("reading restored text checks discovery without storing it again", () =>
        {
            var text = Text();
            text.SetAsTranslated(1);
            Assert(text.Facts.Contains("A") && text.Checks == 1 && text.Stores == 0,
                "Restored-text discovery or storage behavior incorrect");
        });

        Test("reading A does not award B; reading the restored copy at B does", () =>
        {
            var a = Text(false);
            var b = Text(true, NomaiText.Location.B);
            a.SetAsTranslated(1);
            Assert(a.Facts.Contains("A") && b.Facts.Count == 0, "Unvisited location awarded");
            b.SetAsTranslated(1);
            Assert(b.Facts.Contains("B"), "Second location discovery missing");
        });

        Test("silent restoration does not award discoveries", () =>
        {
            var text = Text();
            TranslationUtils.IsSilentlyRestoring = true;
            try { text.SetAsTranslated(1); }
            finally { TranslationUtils.IsSilentlyRestoring = false; }
            Assert(text.Checks == 0 && text.Stores == 0, "Restoration changed progress");
            text.SetAsTranslated(1);
            Assert(text.Facts.Contains("A"), "Subsequent read failed");
        });

        Test("normal translation keeps its existing discovery and storage behavior", () =>
        {
            var text = Text(false);
            text.SetAsTranslated(1);
            Assert(text.Checks == 1 && text.Stores == 1 && text.Facts.Contains("A"),
                "Normal translation changed");
        });

        Test("untranslated required blocks prevent premature discovery", () =>
        {
            var text = Text();
            text.SetState(2, false);
            text.Conditions.Clear();
            text.Conditions.Add((text._location, new[] { 1, 2 }, "Both"));
            text.SetAsTranslated(1);
            Assert(text.Facts.Count == 0, "Unread required block ignored");
            text.SetAsTranslated(2);
            Assert(text.Facts.Contains("Both"), "Second block did not complete discovery");
        });

        Test("repeated reads do not duplicate discoveries, notifications, or storage", () =>
        {
            var text = Text();
            for (int i = 0; i < 10; i++) text.SetAsTranslated(1);
            Assert(text.Facts.Count == 1 && text.Notifications == 1 && text.Stores == 0,
                "Repeated read duplicated progress");
        });

        Test("location changes are evaluated without cached state", () =>
        {
            var text = Text();
            text.Conditions.Add((NomaiText.Location.B, new[] { 1 }, "B"));
            text.SetAsTranslated(1);
            Assert(!text.Facts.Contains("B"), "Wrong-location fact awarded");
            text._location = NomaiText.Location.B;
            text.SetAsTranslated(1);
            Assert(text.Facts.Contains("B"), "Location change ignored");
        });

        Test("newly restored blocks are considered on subsequent reads", () =>
        {
            var text = Text();
            text.SetState(2, false);
            text.Conditions.Clear();
            text.Conditions.Add((text._location, new[] { 1, 2 }, "Both"));
            text.SetAsTranslated(1);
            Assert(text.Facts.Count == 0, "Incomplete condition awarded");
            text.SetState(2, true);
            Assert(text.Facts.Count == 0, "Changing restored state awarded discovery");
            text.SetAsTranslated(1);
            Assert(text.Facts.Contains("Both"), "New restored state ignored");
        });

        Test("invalid block retains existing no-discovery behavior", () =>
        {
            var text = Text();
            text.SetAsTranslated(99);
            Assert(text.Checks == 0 && text.Stores == 0, "Invalid block changed progress");
        });

        Console.WriteLine($"{_passed} checks passed. Behavioral fixtures are not an in-game playtest.");
    }
}
