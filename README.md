# Lingering Translations

## gravewhisper discovery-fix fork

Experimental fix for [upstream issue #1](https://github.com/MegaPiggy/LingeringTranslations/issues/1):
restored projection-stone text can bypass location-specific ship-log discoveries.

This fork rechecks vanilla discovery conditions only when the translator finishes
displaying a read text node. Loading a scene or synchronizing another copy does not
award discoveries. It retains the original mod ID and save format, so it replaces
rather than runs alongside the original mod. Back up the mod directory and game
save before testing. Reinstalling/updating from the official mod database may
replace this fork.

The original bug and disable-mod workaround were manually verified by gravewhisper.
The patch is AI-assisted and has automated regression coverage, but still requires
in-game human verification. See [TESTING.md](TESTING.md) for build commands, test
limitations, and the playtest checklist. Original author: MegaPiggy (MIT license).

## Original description

Keeps previously translated Nomai writing translated/saved across loops.

Once Nomai writing has been translated (greyed out), it will stay that way across loops and can be read immediately without translating it again.

## Mod Compatibility

Also supports modded writing added through [New Horizons](https://outerwildsmods.com/mods/newhorizons/).

## Existing Saves

Writing translated before installing Lingering Translations **must be found and translated again once** before the mod can remember it.

The mod **cannot** determine which Nomai writing you translated before it was installed.
