# Building and testing the discovery fix

## Build

Use a current .NET SDK (verified with 10.0.401):

```sh
dotnet build LingeringTranslations.csproj -c Release -p:OutputPath=bin/Release/
```

The explicit output path overrides the upstream `.csproj.user` setting, which
otherwise attempts to install directly into a Windows mod-manager directory.
Do not build directly into an active game installation.

The output uses the original mod ID, `MegaPiggy.LingeringTranslations`, with version
`1.0.1-gravewhisper.1`. It does not introduce additional game-time dependencies.

## Automated regression checks

On Linux, use Mono to run the .NET Framework test executable:

```sh
dotnet build tests/DiscoveryTests/DiscoveryTests.csproj -c Release
mono tests/DiscoveryTests/bin/Release/net48/DiscoveryTests.exe
```

Optionally check the call signature in your own installed game assembly:

```sh
mono tests/DiscoveryTests/bin/Release/net48/DiscoveryTests.exe \
  "/path/to/Outer Wilds/OuterWilds_Data/Managed/Assembly-CSharp.dll"
```

On Windows, run the test `.exe` directly instead of using `mono`.

The tests compile the production `TranslationDiscoveryPatch.cs` against small
behavioral game fixtures and apply its actual Harmony transpiler. They reproduce
the baseline early-return failure before patching, then exercise the fix, including:

- A completed read of restored text awards the applicable discovery.
- Reading one copy does not award another location's discovery.
- Silent restoration and incomplete display do not award discoveries.
- Conditions with untranslated required blocks remain incomplete.
- Normal translations and virtual overrides retain their behavior.
- Repeated frames do not repeatedly check the same node and location.
- Location changes, XML reloads, new scene instances, and newly synchronized
  blocks can trigger fresh checks.
- Failed checks can be retried; invalid block IDs do not trigger checks.
- The transpiler preserves labels/exception blocks and rejects unexpected call counts.

The optional assembly check reads metadata with Cecil, without executing or
redistributing game code. It verifies that the installed `DisplayTextNode` method
contains exactly one call with the expected signature.

These are not Unity integration tests. The fixtures do not reproduce the complete
translator UI, upstream save/loading lifecycle, all XML condition forms, or other
mods. The optional metadata check does not prove runtime compatibility by itself.

## Manual playtest (still required)

1. Quit the game. Back up the original mod directory (including `config.json` and
   `save.json`) and the game's save directory outside the repository.
2. Replace the original mod's DLL, PDB, and manifest with the build output. Preserve
   the original configuration and translation save, then enable the mod.
3. Launch through the mod manager. Check OWML logs for patch exceptions.
4. Using a save where the relevant discoveries are still missing, translate one
   projection stone at location A. Confirm location B has not been completed by
   this interaction alone.
5. Visit B and read its corresponding text. Confirm B's applicable ship-log entry
   appears. Viewing a projection is a separate interaction and should remain so.
6. Repeat with a restart between A and B. Also test partially read text, switching
   stones in a pedestal, ordinary writing, and another translator-supported text
   display. Confirm repeated reading does not produce repeated notifications.
7. If you use New Horizons, test its generated and dynamically reloaded text too.

Do not erase discoveries from your main save just to reproduce the bug. Use a
separate test profile or a deliberately restored backup instead.

To roll back, quit the game and restore the backed-up DLL/PDB/manifest/config.
Keep the current translation save unless intentionally restoring an earlier
backup. Game discoveries already awarded are not undone by replacing the mod.
