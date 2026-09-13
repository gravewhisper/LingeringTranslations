using HarmonyLib;
using LingeringTranslations;
using Mono.Cecil;
using System;
using System.Linq;
using System.Reflection.Emit;

internal static class Program
{
    private static int _passed;

    private static NomaiText Text(bool restored = true, NomaiText.Location location = NomaiText.Location.A)
    {
        var text = new NomaiText { _location = location };
        text.Translated.Add(1, restored);
        text.Conditions.Add((location, new[] { 1 }, location.ToString()));
        return text;
    }

    private static void Read(NomaiText text, int id = 1, bool complete = true) =>
        new NomaiTranslatorProp { Text = text, Id = id, Complete = complete }.Display();

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

    private static void MustThrow(Action action)
    {
        try { action(); }
        catch (InvalidOperationException) { return; }
        throw new Exception("Expected InvalidOperationException");
    }

    public static void Main(string[] args)
    {
        // Demonstrate the reported failure before applying the real production transpiler.
        Test("baseline reproduced: restored text skips discovery", () =>
        {
            var text = Text();
            Read(text);
            Assert(text.Checks == 0 && text.Facts.Count == 0, "Baseline did not reproduce");
        });

        new Harmony("gravewhisper.LingeringTranslations.Tests").PatchAll(typeof(TranslationDiscoveryPatch));

        Test("completed read awards restored text's discovery", () =>
        {
            var text = Text();
            Read(text);
            Assert(text.Facts.Contains("A") && text.Checks == 1, "Discovery missing");
        });

        Test("reading A does not award unvisited B; reading B does", () =>
        {
            var a = Text(false);
            var b = Text(true, NomaiText.Location.B); // Represents the synchronized copy.
            Read(a);
            Assert(a.Facts.Contains("A") && b.Facts.Count == 0, "Unvisited copy awarded");
            Read(b);
            Assert(b.Facts.Contains("B"), "Second location discovery missing");
        });

        Test("automatic restoration does not award discoveries", () =>
        {
            var text = Text();
            TranslationUtils.IsSilentlyRestoring = true;
            try
            {
                text.SetAsTranslated(1);
                TranslationDiscoveryPatch.OnTextRead(text, 1);
                Assert(text.Checks == 0, "Restoration awarded discovery");
            }
            finally { TranslationUtils.IsSilentlyRestoring = false; }
            Read(text);
            Assert(text.Checks == 1, "Silent call incorrectly cached the read");
        });

        Test("incomplete display never checks discoveries", () =>
        {
            var text = Text();
            Read(text, complete: false);
            Assert(text.Checks == 0, "Incomplete display awarded discovery");
        });

        Test("untranslated required blocks still prevent discovery", () =>
        {
            var text = Text();
            text.Translated.Add(2, false);
            text.Conditions.Clear();
            text.Conditions.Add((text._location, new[] { 1, 2 }, "Both"));
            Read(text);
            Assert(text.Facts.Count == 0, "Unread required block ignored");
            Read(text, 2);
            Assert(text.Facts.Contains("Both"), "New block did not complete discovery");
        });

        Test("normal translation checks once, repeated frames do not add checks", () =>
        {
            var text = Text(false);
            for (int i = 0; i < 10; i++) Read(text);
            Assert(text.Checks == 1, "Repeated or missing native checks");
        });

        Test("restored text checks once across repeated frames", () =>
        {
            var text = Text();
            for (int i = 0; i < 10; i++) Read(text);
            Assert(text.Checks == 1, "Repeated restored-text checks");
        });

        Test("different location on the same component is checked separately", () =>
        {
            var text = Text();
            text.Conditions.Add((NomaiText.Location.B, new[] { 1 }, "B"));
            Read(text);
            Assert(!text.Facts.Contains("B"), "Wrong-location fact awarded");
            text._location = NomaiText.Location.B;
            Read(text);
            Assert(text.Facts.Contains("B") && text.Checks == 2, "Location change was cached out");
        });

        Test("reloading text clears per-component read cache", () =>
        {
            var text = Text();
            Read(text);
            TranslationDiscoveryPatch.Forget(text);
            text.Conditions.Clear();
            text.Conditions.Add((text._location, new[] { 1 }, "Replacement"));
            Read(text);
            Assert(text.Facts.Contains("Replacement"), "Replacement text was cached out");
        });

        Test("newly synchronized block allows rechecking a previously read node", () =>
        {
            var text = Text();
            text.Translated.Add(2, false);
            text.Conditions.Clear();
            text.Conditions.Add((text._location, new[] { 1, 2 }, "Both"));
            Read(text);
            Assert(text.Facts.Count == 0, "Incomplete condition awarded");
            // RestoreTranslation invalidates the cache when it changes a block to translated.
            TranslationDiscoveryPatch.Forget(text);
            text.Translated[2] = true;
            Assert(text.Facts.Count == 0, "Synchronization awarded discovery");
            Read(text);
            Assert(text.Facts.Contains("Both"), "Previously read node was incorrectly cached");
        });

        Test("new scene instance does not inherit read cache", () =>
        {
            Read(Text());
            var reloaded = Text();
            Read(reloaded);
            Assert(reloaded.Checks == 1, "New scene instance was cached out");
        });

        Test("failed discovery check can be retried", () =>
        {
            var text = Text();
            text.ThrowOnCheck = true;
            MustThrow(() => Read(text));
            text.ThrowOnCheck = false;
            Read(text);
            Assert(text.Checks == 1, "Failed read was cached");
        });

        Test("virtual SetAsTranslated overrides are preserved", () =>
        {
            var text = new DerivedNomaiText();
            text.Translated.Add(1, true);
            Read(text);
            Assert(text.VisualUpdates == 1 && text.Checks == 1, "Virtual call lost");
        });

        Test("invalid block does not award discoveries", () =>
        {
            var text = Text();
            Read(text, 99);
            Assert(text.Checks == 0, "Invalid block awarded discovery");
        });

        Test("transpiler preserves labels and exception blocks", () =>
        {
            var instruction = new CodeInstruction(OpCodes.Callvirt,
                AccessTools.Method(typeof(NomaiText), nameof(NomaiText.SetAsTranslated)));
            var label = new DynamicMethod("Labels", typeof(void), Type.EmptyTypes).GetILGenerator().DefineLabel();
            instruction.labels.Add(label);
            instruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
            var result = TranslationDiscoveryPatch.Transpiler(new[] { instruction }).Single();
            Assert(result.opcode == OpCodes.Call && result.labels.Contains(label)
                && result.blocks.Count == 1, "Instruction metadata lost");
        });

        Test("changed or ambiguous game method fails explicitly", () =>
        {
            MustThrow(() => TranslationDiscoveryPatch.Transpiler(Array.Empty<CodeInstruction>()).ToList());
            var original = AccessTools.Method(typeof(NomaiText), nameof(NomaiText.SetAsTranslated));
            MustThrow(() => TranslationDiscoveryPatch.Transpiler(new[]
            {
                new CodeInstruction(OpCodes.Callvirt, original),
                new CodeInstruction(OpCodes.Callvirt, original)
            }).ToList());
        });

        if (args.Length == 1)
        {
            Test("installed game's completed-read method has exactly one matching call", () =>
            {
                // Read metadata only. Do not load or execute the proprietary game assembly.
                using var game = AssemblyDefinition.ReadAssembly(args[0]);
                var method = game.MainModule.Types.Single(t => t.Name == "NomaiTranslatorProp")
                    .Methods.Single(m => m.Name == "DisplayTextNode");
                var calls = method.Body.Instructions.Where(i =>
                    i.OpCode == Mono.Cecil.Cil.OpCodes.Callvirt
                    && i.Operand is MethodReference target
                    && target.DeclaringType.Name == "NomaiText"
                    && target.Name == "SetAsTranslated").ToList();
                Assert(calls.Count == 1, "Game method no longer matches the patch");
                var targetMethod = (MethodReference)calls[0].Operand;
                Assert(targetMethod.Parameters.Count == 1
                    && targetMethod.Parameters[0].ParameterType.FullName == "System.Int32"
                    && targetMethod.ReturnType.FullName == "System.Void", "Call signature changed");
            });
        }

        Console.WriteLine($"{_passed} checks passed. Behavioral fixtures are not an in-game playtest.");
    }
}
