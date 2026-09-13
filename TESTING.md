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
`1.0.1-gravewhisper.2`. It does not introduce additional game-time dependencies.

## Automated regression checks

On Linux, use Mono to run the .NET Framework test executable:

```sh
dotnet build tests/DiscoveryTests/DiscoveryTests.csproj -c Release
mono tests/DiscoveryTests/bin/Release/net48/DiscoveryTests.exe
```

On Windows, run the test `.exe` directly instead of using `mono`.

The tests link production `Patches.cs` and use Harmony to apply its actual
`SetAsTranslatedPrefix` and `SetAsTranslatedPostfix` to a behavioral game fixture.
They first reproduce the unpatched early-return failure, then check:

- Reading restored text awards its applicable location-specific discovery.
- Reading one copy does not award another location's discovery.
- Silent restoration does not award discoveries or store translations again.
- Normal translation preserves its existing discovery and storage behavior.
- Untranslated required blocks prevent premature discovery.
- Repeated reads do not duplicate discoveries, notifications, or storage.
- Location changes and newly restored blocks are considered on subsequent reads.
- Invalid block IDs retain the existing no-discovery behavior.

These are not Unity integration tests. Game discovery/notification behavior and
translation storage are fixtures; the complete translator UI, actual restoration
lifecycle, all XML condition forms, and other mods are not exercised. Unrelated
patches have compile-only stand-ins and are not applied by this test harness.

## Manual playtest (still required)

1. Quit the game. Back up the mod directory (including `config.json` and `save.json`)
   and the game's save directory outside the repository.
2. Replace the mod's DLL, PDB, and manifest with the build output. Preserve the
   configuration and translation save, then enable the mod.
3. Launch through the mod manager. Check OWML logs for patch exceptions.
4. Using a save where the relevant discoveries are still missing, translate a
   projection stone at location A. Confirm this alone does not complete B.
5. Visit B and read its corresponding text. Confirm B's applicable ship-log entry
   appears. Viewing a projection remains a separate interaction.
6. Repeat with a restart between locations and with partially read text. Confirm
   repeated reading does not produce repeated notifications.

Do not erase discoveries from your main save to reproduce the bug. Use a separate
profile or a deliberately restored backup instead.

To roll back, quit the game and restore the backed-up DLL/PDB/manifest/config.
Keep the current translation save unless intentionally restoring an earlier
backup. Game discoveries already awarded are not undone by replacing the mod.
