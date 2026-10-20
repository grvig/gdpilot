# Progress

Read this first in every session, then the plan section for the current phase.

## Current phase

Phase 3 — grid navigation by calibration.

## State

| Phase | Status |
| --- | --- |
| 0. Feasibility probe | Done, HT-1 passed on the scancode rung |
| 1. Input plumbing and safety | Done |
| 2. Mode state machine and overlay | Done, checked live in the game |
| 3. Grid navigation by calibration | In progress: bags calibrated and navigable |
| 4. Occupancy detection | Not started |
| 5. Macros and release | Not started |

## What exists

`GDPilot.exe` reads the pad through SDL and calibrates both sticks at startup.
Each tick it does four things:
- shapes the sticks through radial deadzones;
- maps them to eight-way WASD and a curved cursor;
- tracks which panel is open and applies that panel's button table, including
  the left-trigger chord layer;
- injects only what changed, as scan codes.

Mode changes:
- **Panel bindings:** they tap the panel's key and toggle its mode.
- **Back:** it taps Escape and closes one level.
- **Panic resync:** holding Start and Back clears to Combat.

A click-through overlay in a corner of the game shows the mode, the active
bindings and any suspension.

Safety:
- Ctrl+Alt+Pause toggles a kill switch on its own thread.
- A watchdog releases held input if the loop stalls.
- Injection pauses while the game is not in front.
- Every exit path releases held input.

`--dry-run` prints what would be sent instead of sending it.

## Phase 3 checklist

| Task | Status |
| --- | --- |
| `--capture` mode | Done: saves the game screen and its display profile |
| HT-2 captures | Bags and vendor done; stash not yet |
| Grid maths: centres, clamped steps, pitch from corners | Done: `GridSpec` |
| Profiles and startup validation | Done: `GridReadiness` refuses grids on any mismatch, with a reason |
| `grid:` bindings and navigation | Done for the bags: `GridNavigator`, `grid:next` between surfaces |
| Bag grids for 1920×1080 at UI scale 0.5 | Done: measured, checked live and by fixture test |
| Vendor-mode grids | Not started; the bags move when the trade window is open |
| Stash grids | Not started; needs a capture |
| Modifier clicks for splitting and transfer; bumpers for stash tabs | Not started |
| Calibration wizard and automatic grid detection | Not started |
| `docs/calibration.md` | Started; kept current as calibration is built |

## Live check in the game, 2026-10-19

Grim Dawn v1.3.1.1, borderless 1920×1080, Devil's Crossing.

- A 40 ms scan code tap of I, exactly what a panel binding sends, opened the
  inventory. A 40 ms tap of Escape closed it. Taps of that length are enough.
- The overlay drew over the game in the top-left corner, legibly, and the game
  kept focus while it ran. The host ran at 259 Hz.
- **The character sheet and the inventory are one window in this game,** bound
  to both C and I. The default panel keys no longer list Character, so the two
  can never be tracked as separate panels.
- **In-game keybindings changed for keyboard movement:**

  | Key | Now | Was |
  | --- | --- | --- |
  | W | Forward | Switch Weapons |
  | S | Backward | Skill Window |
  | A | Left | — |
  | D | Right | Drop Item |
  | K | Skill Window | Group Window |

  Switch Weapons, Drop Item and Group Window are now unbound.

## Verified

- Grim Dawn moves on injected scan codes and opens and closes panels on 40 ms
  scan code taps.
- The kill switch releases a held key at operating system level, fires on an
  injected chord, and ignores key repeat.
- The overlay window is click-through, no-activate, layered and topmost,
  checked through its real window styles, and it opens and closes repeatedly.
- Screen capture agrees pixel for pixel with an independent GDI+ capture, and
  absolute coordinates are correct on this 125% scaled display.
- The real pad, an "Xbox 360 Controller" in X mode, reads and calibrates
  through SDL.
- Scripted traces replay through the real mapper and mode controller into
  exact key sequences, including a round trip through the inventory.

- The bag calibration lands on real cells. On 2026-10-20 the cursor at the
  computed centre of the main bag's top-left cell showed its item's tooltip,
  "Scrap (54)", and the second bag's cell (1, 1) showed the chest armour
  stored there. A fixture test checks the shipped numbers against the
  capture, and fails if the origin moves by half a cell.

242 automated tests, all passing.

Not yet verified:
- **Live pad movement.** The sticks rest at exactly 0.0000, so a firmware
  deadzone or a reading that never updates are both possible. Moving a stick
  during `--dry-run` settles it.
- **The pad's D mode.**
- **A real button press reaching the game end to end.** Each link is checked
  separately, but nobody has pressed the pad with the game running.

## Known issues

None open.

## Blocked

Nothing.

## Human tasks

| Task | Phase | Status |
| --- | --- | --- |
| HT-1 Confirm the game accepts synthesized input | 0 | Passed 2026-10-12 |
| HT-2 Capture calibration fixtures | 3 | Bags and vendor captured 2026-10-20, unattended; stash outstanding |
| HT-3 Feel tuning | 5 | Not yet raised |

## Next session starts here

The rest of Phase 3, section 11 of the plan:

1. Measure the vendor-mode grids from `tests/fixtures/screens/vendor.png`:
   the vendor's goods and the bags, which sit elsewhere in that mode. Measure
   them the same way, from line peaks, and add fixture tests.
2. Capture the stash. Finding it by walking blind with keyboard movement was
   unreliable on 2026-10-20. Locate it first, for example from the world map,
   then capture it and add its grids.
3. Modifier-held clicks for stack splitting and quick transfer, and the
   bumpers for stash tabs.
4. The calibration wizard, and automatic grid detection from a capture. The
   line-peak method used by hand is the obvious start.

