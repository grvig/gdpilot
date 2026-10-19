# Progress

Read this first in every session, then the plan section for the current phase.

## Current phase

Phase 2 — mode state machine and overlay.

## State

| Phase | Status |
| --- | --- |
| 0. Feasibility probe | Done, HT-1 passed on the scancode rung |
| 1. Input plumbing and safety | Done |
| 2. Mode state machine and overlay | In progress |
| 3. Grid navigation by calibration | Not started |
| 4. Occupancy detection | Not started |
| 5. Macros and release | Not started |

## HT-1 result, 2026-10-12

| Rung | Idle score | Moved score | Outcome |
| --- | --- | --- | --- |
| virtual key | 5.77 | 5.81 | Fail |
| scancode | 5.53 | 42.40 | Pass |

Grim Dawn v1.3.1.1 x64, borderless 1920×1080, Keyboard movement, Devil's
Crossing. The character was seen to walk. Gameplay reads scan codes only.

## What Phase 1 delivered

`GDPilot.exe` reads the pad through SDL and calibrates both sticks at startup.
Each tick it shapes the sticks through radial deadzones, maps them to eight-way
WASD and a curved cursor, resolves the combat button table and its
left-trigger chord layer, and injects only what changed, as scan codes.

The safety layer is complete:
- Ctrl+Alt+Pause toggles a kill switch that releases everything held. It runs
  on its own thread and ignores key repeat.
- A watchdog releases held input if the loop stalls.
- Injection is suspended while the game is not in front, and resumes with any
  still-held keys pressed again.
- Every exit path releases held input.

`--dry-run` prints intents instead of injecting them.

## Phase 2 checklist

| Task | Status |
| --- | --- |
| Mode stack: push, pop, clear to combat | Done: `ModeStack`, where a panel's own key toggles it |
| Panel bindings that send the panel key and push the mode | Done: `panel:Inventory`, keys in the `panels` section |
| Escape pops one level and forwards the key | Done: `back`; from Combat it opens the menu |
| Panic resync: Start plus Back held half a second | Done in `ModeController` |
| Per-mode binding tables from the configuration | Done; a mode without a section uses Combat's |
| Cross-mode trace fixture | Done: open the inventory, click, back out |
| Overlay view-model | Done: `OverlayState` |
| Overlay window: layered, transparent, no-activate, topmost | Not started |

## Verified

- Key and mouse injection reach the operating system. Grim Dawn moves on
  injected scan codes.
- The kill switch releases a held key at operating system level, fires on an
  injected chord, and fires once per press despite key repeat.
- Screen capture agrees pixel for pixel with an independent GDI+ capture, and
  absolute coordinates are correct on this 125% scaled display.
- The real pad, an "Xbox 360 Controller" in X mode, reads through SDL and
  calibrates. The host ran against it at 251 Hz with 250 configured.
- Scripted traces replay through the real mapper into exact key sequences.
- The control loop presses a still-held stick again after a suspension.

- Mode transitions: double opens, closing a panel below the top, Escape at
  depth zero, held buttons acting once, taps held 40 ms, panic resync and a
  brief Start plus Back that is not one.

203 automated tests, all passing.

Not yet verified:
- **Live pad readings.** Both sticks rest at exactly 0.0000, which a firmware
  deadzone explains, but so would a reading that never updated. Moving a stick
  during `GDPilot.exe --dry-run` settles it.
- **The pad's D mode.**
- **Play in the game itself.** That needs in-game keybindings first: only
  Forward is bound, to W. Backward, Left and Right need S, A and D, and S
  currently opens the Skill window.

## Known issues

None open.

## Blocked

Nothing.

## Human tasks

| Task | Phase | Status |
| --- | --- | --- |
| HT-1 Confirm the game accepts synthesized input | 0 | Passed 2026-10-12 |
| HT-2 Capture calibration fixtures | 3 | Not yet raised |
| HT-3 Feel tuning | 5 | Not yet raised |

## Next session starts here

1. The overlay window in a new `GDPilot.Overlay` WPF project. It is layered,
   transparent, no-activate, topmost and click-through, follows the game
   window's bounds, and draws `OverlayState`. Smoke-test that it opens and
   closes, then host it from `GDPilot.App`.
2. A live check in the game: bind S, A and D in-game, run the host, and
   confirm panels open with the tracked mode matching. Taps are held 40 ms,
   and whether the game needs more is unknown.
3. Then Phase 3.
