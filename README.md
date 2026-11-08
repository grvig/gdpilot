# GDPilot

Play Grim Dawn properly on a controller.

Grim Dawn's own gamepad mode drags a fake mouse pointer around the menus, can
only reach part of your hotbar, and keeps the camera stuck up in the sky.
GDPilot replaces all of that. You get a third-person camera, lock-on, the whole
hotbar on your pad, and menus where the D-pad jumps straight to items instead of
nudging a cursor around.

Windows only. Pre-release: see [Known issues](#known-issues) before you start.

## Features

**Combat**
- Third-person camera behind your character, turned and zoomed with the right
  stick. It stays clear of walls, and one button switches back to the normal
  top-down view.
- Lock-on with a right stick click. Flick the stick to switch targets. When a
  target dies, the lock moves to the next enemy.
- 360° movement. The left stick walks at its actual angle, not just eight
  directions.
- Twin-stick aiming in the normal camera, with soft aim that pulls your
  attacks onto the enemy you're roughly pointing at.
- Dodge on B. Put a movement skill in slot 4 and it fires the way you're
  pushing the stick.
- All ten hotbar slots: eight on the face buttons (with LT as a second layer),
  plus a radial wheel on Back that shows your actual skill icons.
- A on its own picks up loot, talks to people, opens chests and uses rift
  gates, whatever is nearest.
- Rumble for heavy hits, kills and low health.

**Menus**
- Bags, stash and traders: the D-pad jumps from item to item and skips empty
  cells. A big item counts as one stop.
- The skill tree, devotion map, conversations, blacksmith and game menu all
  snap to the next thing in the direction you press.
- Each window gets its own button layout as soon as it opens, and a small
  card in the corner shows what every button does.

**Other**
- Rift travel from anywhere on LT + LB.
- Different bindings for each character, and settings that apply as soon as
  you save them.
- Sits in the tray and can start with Windows.

## Install

1. Download `GDPilot-0.9.0.zip` from
   [Releases](https://github.com/grvig/gdpilot/releases) and unzip it anywhere.
2. In Grim Dawn's options:
   - Movement: **Keyboard** (not Keyboard Only), with WASD for movement. S
     opens the skill window by default, so move that to K.
   - Turn the game's own gamepad support off. Turn Steam Input off for Grim
     Dawn too.
   - Display: borderless windowed.
3. Run `GDPilot.exe`. The first run checks your setup and tells you anything
   to change, including keybinds. After that it runs from a small icon by the
   clock (look under the ^ arrow). Right-click it for settings or to quit.
4. Start the game. GDPilot hooks in by itself about 20 seconds after the game
   window appears.

If you'd rather the game load GDPilot's camera part on its own, run
`GDPilot.exe --install-loader` once. It adds a small `dinput8.dll` to the
game's x64 folder and changes nothing else. `--remove-loader` takes it out.

## Controls

| Button | In the world |
| --- | --- |
| Left stick | Walk |
| Right stick | Aim (normal camera) / look around (third person) |
| RB / LB | Attack / right click |
| A | Interact with whatever is nearest |
| X, Y | Slots 2, 3 |
| B | Dodge (slot 4) |
| LT + A, X, Y, B | Slots 1, 5, 6, 7 |
| Back (hold) | Skill wheel |
| Right stick click | Lock on |
| LT + right stick click | Third-person camera on/off |
| LT + LB | Personal rift |
| D-pad | Inventory, devotion, skills, map |
| Start | Close the top window, or the game menu |

Everything can be rebound in `config.json` (right-click the tray icon to open
it). Full list: [docs/controls.md](docs/controls.md).

**Ctrl+Alt+Pause stops all input instantly**, even if the game freezes. Press it
again to resume. GDPilot also lets go of everything whenever the game isn't the
window in front. No Pause key on your laptop? Change `killSwitch` in
`config.json`, or pause from the tray icon.

## Known issues

This is a pre-release. Most of it has been tested in-game, but:

- It hasn't had much play time on a real pad yet. Stick feel, dead zones and
  rumble strength may need tuning, all in `config.json`.
- Unplugging the pad mid-fight is handled but hasn't been tried for real.
- The camera and item features need the in-game part. If a game patch breaks
  it, GDPilot turns that part off and falls back to the normal camera and
  reading the screen. `GDPilot.exe --check-game` tells you if a new game
  version is still supported.

Bugs and feedback: [Issues](https://github.com/grvig/gdpilot/issues).

## Requirements

- Windows 11, 64-bit (Windows 10 should work but is untested)
- Grim Dawn 1.3 or later, 64-bit Steam version
- An Xbox or PlayStation style controller

## Fair play

GDPilot moves the camera and reads where things are. It never changes damage,
drops, stats or anything else about how the game plays, and it doesn't modify
any game files.

Antivirus software sometimes flags tools that load into a game. The full source
is here if you want to check what it does.

## Building from source

Needs the .NET 8 SDK and Visual Studio's C++ build tools. `publish.cmd` builds
the zip. `dotnet run --project src/GDPilot.App -- --dry-run` prints what would
be sent instead of sending it. [PROGRESS.md](PROGRESS.md) and
[PLAN.md](PLAN.md) cover where development is at.

## Credits

The camera builds on [GrimAction](https://github.com/Ophemist/GrimAction) by
Ophemist, used under its MIT licence.

Not affiliated with or endorsed by Crate Entertainment.

## Licence

MIT. See [LICENSE](LICENSE).
