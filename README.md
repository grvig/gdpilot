# GDPilot

A controller-first overhaul for Grim Dawn: a third-person action camera,
lock-on, radial skills, menus that snap to items, and controls that adapt to
what's on screen. For Windows.

Grim Dawn was built for a mouse. Its own gamepad support steers a fake mouse
pointer around menus, can't reach most of the hotbar, and keeps the camera high
above the character. GDPilot turns it into a game you play with a controller
from start to finish: a camera behind your character that you aim with the
right stick, skills on the face buttons and a radial wheel, and inventory,
stash and trading where the D-pad lands on items instead of pixels.

## What GDPilot will do when it is finished

### Play

- **A third-person action camera.** Drop the camera behind your character, over
  either shoulder, and look around with the right stick. It stays out of walls,
  and a button swaps back to the classic top-down view at any time.
- **Lock-on.** Click the right stick to lock onto the enemy you face; flick
  the stick to switch targets. Skills fire at whatever you have locked, the
  camera turns to keep it in view, and when it falls the lock moves on.
- **Movement that follows the camera.** Push the stick away from you and you
  walk away from the camera, however it is turned.
- **Every skill within reach.** The four face buttons are instant skills, with
  four more on the left trigger plus face. Everything else on the hotbar sits on
  a radial wheel: hold View, flick the right stick, release to cast, and keep
  walking with the left stick while you choose. The wheel shows your real skill
  icons, copied from the hotbar, and a face button pressed while pointing at a
  slot swaps that skill onto it.
- **One button for the obvious thing.** Pick up the nearest loot, talk to the
  nearest character or open the nearest chest without aiming at it.
- **Rumble that means something.** Low health, heavy hits and rare drops each
  have their own feel.

### Menus

- **Bindings that follow the open panel.** Combat, Inventory, Stash, Skills,
  Devotion, WorldMap, Vendor and Dialog each have their own button table, and
  opening a panel switches to it automatically.
- **Inventory, stash and trading that snap to items.** The D-pad jumps to the
  next item in that direction and skips empty cells. A sword three cells tall
  is one stop. Buttons click, split stacks and switch stash tabs.
- **Snapping everywhere else.** Skill tree nodes, devotion stars, dialogue
  choices and the blacksmith's recipe list, all reachable with the D-pad.
- **Shortcuts.** Fast travel to the nearest rift gate, selling a marked group of
  items in one sweep, and town portal and return, each on one button.
- **An overlay that teaches as you go.** A small card in a corner shows the
  current mode and what every button does there, with Xbox or PlayStation icons
  to match your pad.

### Setup

- **One install.** One download. GDPilot starts with Windows or by hand and
  sits in the tray; when Grim Dawn starts, it connects by itself.
- **Profiles per character**, and settings that change while you play.
- **A guided first run** that sets up the game and calibrates the menus.

### Safety

- Ctrl+Alt+Pause stops everything at once, even if the game itself has frozen.
- Input pauses whenever the game is not the window in front.
- A watchdog lets go of any held key, so no key is ever left stuck down.

## How it works

GDPilot is one install with two parts inside.

- **The controller side** runs beside the game. It reads the pad, presses keys
  and moves the mouse the way a player would, draws the overlay and owns the
  kill switch. It never needs the game's internals, so a game update cannot
  break it.
- **The in-game part** loads into Grim Dawn when it starts. It moves the camera,
  and reports what only the game knows: exactly where every item sits, which
  panel is open, and where enemies are. It finds what it needs inside the game
  by name, the way the game's own files label their functions, so most patches
  should not stop it.

If an update ever does stop the in-game part, it switches itself off and the
controller side carries on: the classic camera comes back and menus are read
off the screen instead. Nothing freezes and nothing guesses. `GDPilot.exe
--check-game` says straight away whether a new game version is still covered.

## What it will never do

- Change damage, drops, stats, balance or progression. The in-game part moves
  the camera and reads positions; it changes nothing about how the game plays
  out.
- Modify or replace the game's files or archives.
- Redistribute any game assets.
- Give an edge in multiplayer beyond playing comfortably with a controller.

## Where it stands

| Phase | | Status |
| --- | --- | --- |
| 0 | Proof that Grim Dawn accepts synthesized input | **Done** |
| 1 | Pad input, movement, cursor, buttons, safety | **Done** |
| 2 | Panel tracking, per-panel bindings, overlay | **Done** |
| 3 | Grid navigation by calibration | **Done.** Bags, vendor and stash, checked in the game. |
| 4 | Reading item positions | **Done.** From the game itself, exactly, with the screen as a fallback. |
| 5 | The in-game part and the action camera | **Done.** Third person with turning, zoom, collision and following, all from the pad. |
| 6 | Lock-on, camera-relative movement, smart interact | **Mostly done.** Lock-on and movement that follows the camera work; smart interact to come. |
| 7 | Radial skills, rumble, snapping in every menu | **Started.** The skill wheel and rumble work; snapping in the other menus to come. |
| 8 | Shortcuts, profiles, first run, tray, release | Not started |

**Working today:**

- **Lock-on.** Click the right stick to lock onto the enemy you face. A ring
  marks it with a bar of its life, the cursor stays on it so every attack and
  skill lands on it, and in third person the camera glides round to keep it in
  view. Flick the stick to switch to the next enemy that way; when one falls,
  the lock moves to the next. Enemies behind walls are never offered. See
  [docs/controls.md](docs/controls.md).
- **The third-person camera.** Hold the left trigger and click the right stick
  and the camera drops behind your character. The right stick turns and zooms it, walking turns
  with it, it swings round as you walk sideways, and it stays in front of
  walls and clutter. Do it again for the classic view, restored exactly. See
  [docs/camera.md](docs/camera.md).
- **The skill wheel.** Hold Back for a wheel of the whole hotbar, drawn with
  your real skill icons; point the right stick and let go to cast.
- **Rumble** for heavy blows, and a heartbeat when life runs low.
- **GDPilot always knows which panel is open**, because the game tells it, and
  an item larger than one cell is one stop on the D-pad.

[docs/controls.md](docs/controls.md) lists every control.

- The left stick walks and the right stick drives a curved cursor; every button
  is configurable, with a second layer on the left trigger.
- The D-pad opens panels in combat, and each panel switches to its own buttons.
- In the bags, the stash and while trading, the D-pad jumps from item to item
  over empty cells, the bumpers switch stash tabs, and LT+A splits stacks. This
  is calibrated for 1920×1080 at UI scale 0.5; see
  [docs/calibration.md](docs/calibration.md), and `--calibrate` for any other
  display.
- The overlay, the kill switch, the focus pause and the stuck-key watchdog.
- `--replay` plays a recorded pad performance through the whole program, and
  `--check-game` checks an installed game version.

It is a console program built from source for now. [PROGRESS.md](PROGRESS.md)
has the detail, and [PLAN.md](PLAN.md) the full design.

## Trying it now

You need the .NET 8 SDK. From the repository root:

```
dotnet build
dotnet run --project src/GDPilot.App -- --dry-run
```

`--dry-run` prints what would be sent instead of sending it, so it is safe with
the game closed. Without it, GDPilot drives the game whenever the game is in
front.

Other commands:

| Command | What it does |
| --- | --- |
| `--load-in-game` | Loads the in-game part into the running game, for the third-person camera; see [docs/camera.md](docs/camera.md) |
| `--check-game` | Checks an installed game version still has everything the in-game part needs |
| `--targets` | Prints the enemies the in-game part sees, and which one lock-on would take |
| `--calibrate <Grid>` | Measures a menu grid for a new resolution or UI scale |
| `--occupancy <Grid>` | Prints which cells of an open grid hold items |
| `--replay <trace>` | Plays a recorded pad performance instead of reading the pad |

Settings live in `config.json` beside the executable or in the working
directory. Copy [config.example.json](config.example.json) as a starting point.

GDPilot expects the game set up like this:

- **Movement type:** Keyboard (not Keyboard Only), with Forward, Backward, Left
  and Right on W, S, A and D. S opens the skill window by default, so move that
  to another key, such as K.
- **Gamepad:** in-game controller support off, and Steam Input off for Grim
  Dawn.
- **Display:** borderless windowed.

## Requirements

- Windows 11 x64; Windows 10 will probably work but is untested
- Grim Dawn v1.3 or later, the 64-bit Steam version
- An Xbox or PlayStation layout gamepad; both work through SDL

## Credits

The in-game part builds on the camera work of
[GrimAction](https://github.com/Ophemist/GrimAction) by Ophemist, used under
its MIT licence.

## Disclaimer

GDPilot is an unofficial tool. It is not affiliated with, endorsed by, or
associated with Crate Entertainment. Antivirus programs can flag any tool that
loads into another program; GDPilot's source is public so anyone can check
what it does.

## Licence

MIT. See [LICENSE](LICENSE).
