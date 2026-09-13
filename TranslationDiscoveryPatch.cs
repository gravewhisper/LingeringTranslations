using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace LingeringTranslations;

[HarmonyPatch(typeof(NomaiTranslatorProp), "DisplayTextNode")]
internal static class TranslationDiscoveryPatch
{
    private static readonly ConditionalWeakTable<NomaiText, HashSet<(NomaiText.Location, int)>> _checkedReads = new();

    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var code = instructions.ToList();
        var original = AccessTools.Method(typeof(NomaiText), nameof(NomaiText.SetAsTranslated));
        var calls = code.Where(instruction => instruction.Calls(original)).ToList();
        if (calls.Count != 1)
        {
            throw new InvalidOperationException(
                $"Expected one completed-read SetAsTranslated call in DisplayTextNode, found {calls.Count}.");
        }

        // Replace only the completed-read call, not scene loading or cross-copy restoration.
        // Retain the instruction's labels and exception blocks.
        calls[0].opcode = OpCodes.Call;
        calls[0].operand = AccessTools.Method(typeof(TranslationDiscoveryPatch), nameof(OnTextRead));
        return code;
    }

    internal static void OnTextRead(NomaiText text, int id)
    {
        bool wasTranslated = text.IsTranslated(id);
        text.SetAsTranslated(id);

        if (TranslationUtils.IsSilentlyRestoring || !text.IsTranslated(id))
        {
            return;
        }

        var checkedReads = _checkedReads.GetOrCreateValue(text);
        var key = (text._location, id);
        if (checkedReads.Contains(key))
        {
            return;
        }

        // Vanilla returns early for restored text, skipping its location-sensitive discovery check.
        // New translations already ran that check in SetAsTranslated.
        if (wasTranslated)
        {
            text.CheckSetDatabaseCondition();
        }

        checkedReads.Add(key);
    }

    internal static void Forget(NomaiText text)
    {
        // A pedestal can load different text into the same component.
        _checkedReads.Remove(text);
    }
}
