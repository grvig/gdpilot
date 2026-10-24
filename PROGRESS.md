# Progress

Read this first in every session, then the plan section for the current phase.

## Current phase

Phase 4 — occupancy detection. Phase 3 is done apart from one check that
needs a person at the pad; see "Not yet verified".

## State

| Phase | Status |
| --- | --- |
| 0. Feasibility probe | Done, HT-1 passed on the scancode rung |
| 1. Input plumbing and safety | Done |
| 2. Mode state machine and overlay | Done, checked live in the game |
| 3. Grid navigation by calibration | Done, checked live in the game end to end through `--replay` |
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
| HT-2 captures | Done: bags, vendor and stash |
| Grid maths: centres, clamped steps, pitch from corners | Done: `GridSpec` |
| Profiles and startup validation | Done: `GridReadiness` refuses grids on any mismatch, with a reason |
| `grid:` bindings and navigation | Done: `GridNavigator`, `grid:next` between surfaces |
| Bag grids for 1920×1080 at UI scale 0.5 | Done: measured, checked live and by fixture test |
| Vendor-mode grids | Done: goods and both bags, fixture-tested, checked live by tooltip |
| Stash grids and tab strip | Done: stash, both bags and a two-tab column, measured live by the wizard and fixture-tested |
| Modifier clicks for splitting | Done: LT+A sends Ctrl+click, which opened the split dialog live while trading and at the stash |
| Transfer clicks | Not shipped: right click equips or uses an item; it does not move it to the stash |
| Bumpers for stash tabs | Done: `tab:next` and `tab:previous` click through `Stash.Tabs`, a row or a column; checked live |
| Automatic grid detection | Done: `GridDetector`, and `--detect` on a saved capture |
| Calibration wizard | Done: `--calibrate GridName` from the pad, or `--at` and `--counts` without one; the latter run live |
| `--replay` | Done: drives the real host from a recorded pad trace, for end-to-end checks with nobody at the pad |
| `docs/calibration.md` | Done for everything built so far |

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

- Grid detection finds all eight grids in the three captures on its own, and
  the wizard's logic, fed those captures, lands on each grid from points a few
  pixels off its corner cells.

- **2026-10-24, live in the game, unattended:**
  - Tooltips under the computed centres matched in every mode: the trader's
    first item and both bags while trading, the stash's first item and both
    bags at the stash.
  - Ctrl+click on a stack opened the split dialog, while trading and at the
    stash.
  - The wizard, run with `--at` on the open stash, read the stash grid, the
    first bag and, with `--counts`, the tab column off the screen, and saved
    exactly the numbers now shipped. That run found and fixed an off-by-one
    in its screen capture.
  - GDPilot itself, fed `tests/fixtures/traces/stash-tour.json` through
    `--replay`, entered Stash mode with the overlay up, stepped the cells,
    switched to tab 2 and back with the bumpers, cycled every grid, and opened
    the split dialog with LT+A. A test pins what that trace sends.
  - Clicking a locked tab, or the coin button below the tabs, offers to buy a
    tab. One accepted prompt bought tab 2 for 25,000 iron bits of the test
    character's money; another was declined. A ring equipped by a right-click
    test was swapped back.

255 core tests and 48 integration tests, all passing.

Not yet verified:
- **Live pad movement.** The sticks rest at exactly 0.0000, so a firmware
  deadzone or a reading that never updates are both possible. Moving a stick
  during `--dry-run` settles it.
- **The pad's D mode.**
- **A real button press reaching the game end to end.** Each link is checked
  separately, but nobody has pressed the pad with the game running.
- **The pad-driven wizard in the game.** Its steps run live through `--at`;
  the stick-and-button loop around them needs someone at the pad.

## Known issues

None open.

## Blocked

Nothing.

## Human tasks

| Task | Phase | Status |
| --- | --- | --- |
| HT-1 Confirm the game accepts synthesized input | 0 | Passed 2026-10-12 |
| HT-2 Capture calibration fixtures | 3 | Done: bags and vendor 2026-10-20, stash 2026-10-24, unattended |
| HT-3 Feel tuning | 5 | Not yet raised |

## Next session starts here

Phase 4, section 12 of the plan: occupancy detection.

1. Decide how an empty cell is recognised. The plan stores the empty-slot
   colour at calibration time; the captures show empty cells as dark interiors
   inside bright lines, so a dark-centre test may need no stored colour at all.
2. Build the occupancy grid in Core from a frame and a `GridSpec`, tested on
   synthetic bitmaps first, then against hand-labelled occupancy for the three
   captures in `tests/fixtures/screens`.
3. Group multi-cell items into single targets, then move the D-pad to the
   nearest occupied target, falling back to Phase 3 stepping on low confidence.

When someone is at the pad, also settle "Not yet verified": move the sticks
during `--dry-run`, try D mode, and run `--calibrate` with the stick.
