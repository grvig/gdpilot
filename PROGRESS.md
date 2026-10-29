# Progress

Read this first in every session, then the plan section for the current phase.

## Current phase

Phases 6 and 7, PLAN.md section 17: lock-on, smart interact and snapping in
every menu. Phase 5 is done, and parts of 6 and 7 with it. The project was
widened on 2026-10-25 into a controller-first overhaul; section 17 of the plan
holds the phases and the rules for the in-game part.

## State

| Phase | Status |
| --- | --- |
| 0. Feasibility probe | Done, HT-1 passed on the scancode rung |
| 1. Input plumbing and safety | Done |
| 2. Mode state machine and overlay | Done, checked live in the game |
| 3. Grid navigation by calibration | Done, checked live in the game end to end through `--replay` |
| 4. Occupancy detection | Done: exact item layouts from the game, checked live; screen reading as the fallback |
| 5. In-game part and action camera | Done: camera, turning, zoom, collision, following, panel and item reports, all checked live |
| 6. Lock-on, camera-relative movement, smart interact | Mostly done: lock-on and camera-relative movement checked live; smart interact not started |
| 7. Radial skills, rumble, snapping in every menu | Started: the skill wheel with icons checked live; rumble built, health checked live; snapping in other menus not started |
| 8. Shortcuts, profiles, first run, release | Not started |

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

## Phase 4 checklist

| Task | Status |
| --- | --- |
| Recognise empty cells | Done: fixed brightness and colour thresholds, no stored colour; see DECISIONS |
| Occupancy grid from the screen | Done: `OccupancyMap`, read per press by `ScreenOccupancy` for one grid's rectangle |
| Synthetic tests | Done: line-drawn panels with items, larger cells, doubtful readings, covered cells |
| Hand-labelled captures | Done: `tests/fixtures/screens/occupancy.txt`, all eight grids read back cell for cell |
| Cells under a tooltip or carried item | Done: keep their last trusted state unless they now read as an item |
| Jump to the nearest item in a direction | Done: checked live, the cursor skipped empty cells in the second bag |
| Fall back to stepping, shown on the overlay | Done: past a tenth unsure, with "Grid: stepping, screen unclear" |
| `--occupancy` diagnostic | Done |
| Multi-cell items as one target | Done from the game's rectangles (`OccupancyMap.FromItems`); the screen fallback keeps every cell a stop, since edge colour merged same-coloured neighbours |

## Phase 5 groundwork, 2026-10-25

| Piece | Status |
| --- | --- |
| Report format | Done: `GameReport`, one-line JSON with a protocol number; open panels and item layouts |
| The connection | Done: `GameLink`, a named pipe for the current user only, reconnecting, dropping every report on disconnect |
| Reports before the screen | Done: `ReportedOccupancy` uses the game's layout when it fits the calibrated grid, otherwise the screen |
| Function list and game check | Done: `game-functions.txt`, 33 names; `--check-game` found them all in the installed build |
| Native toolchain | Done: Visual Studio 2022 Build Tools, installed with the owner's approval on 2026-10-25 |

## Phase 5, 2026-10-26

| Piece | Status |
| --- | --- |
| In-game part build | Done: `src/GDPilot.InGame/build.cmd`, Detours fetched at a pinned commit and fingerprint |
| Loading it | Done: `--load-in-game` checks the game, then loads a content-named copy so rebuilds never collide |
| Start-up checks | Done: three camera functions found by name, their first bytes matched; off on any difference |
| Third-person camera | Done: F8 or the right-stick click; the game's camera restored exactly on the way back; `camera.ini` tunes it live |
| Turning with the right stick | Done: look rates down the pipe, applied with the game's `AdjustYaw`; movement follows the camera |
| Aim point | Done: the cursor waits ahead of the character in third person |
| Two-way pipe | Done: buffered, overlapped on the game's side; the camera stops on any suspension |
| Panel and item reports from the game | Done 2026-10-27: panels from UI flags, the mode follows them; bags as exact rectangles |
| Camera collision, stick tilt, follow | Done: collision through the level query, zoom on the stick's up and down, following a sideways walk |

## 2026-10-27 and 2026-10-28

| Piece | Status |
| --- | --- |
| Panel reports | Done: inventory, skills, map, dialogue and the menu, read live as each opened and closed |
| The mode following the game | Done: panels opened and closed by keyboard were followed, live |
| Exact bag layouts | Done: 59 and 9 items read live; the bag trace's sword is one stop |
| Camera collision | Done: seen pulling the camera in front of the town's clutter |
| Stick zoom, camera following | Done, both checked live |
| Skill wheel | Done: held on Back, real icons from the hotbar, the pointed slot lit, live |
| Health and rumble | Done: health read live at 100%; `--rumble-test` ran on the pad; the low-health beat is tested in Core only |
| Default `camera.ini` | Done: written with notes on first load, checked live |
| `--check-game` | 42 functions, all present; it also finds the game through Steam when it is closed |

## Lock-on, 2026-10-29

| Piece | Status |
| --- | --- |
| Targets from the game | Done: the game's own target query, foes only, drawn onto the screen, thirty times a second |
| Sight lines | Done: enemies behind the town wall are never offered, checked by turning a full circle live |
| Choosing, flicking, falls and breaks | Done in Core, tested; flicking right and left switched live |
| The camera turning to the lock | Done: measured easing, locked at the screen's edge and settled in the middle live, without overshoot |
| Cursor, marker and life bar | Done: the marker sat on a Walking Dead in Burial Hill and the attacks landed on it, live |
| Rumble ticks | Built and tested in Core; not yet felt |
| `--check-game` | 49 functions, all present |

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

- **2026-10-25, live, unattended:** `--occupancy` read both bags off the
  screen correctly and confidently. `tests/fixtures/traces/bag-jumps.json`,
  replayed into the game, took the cursor to (0,0), (0,1), (0,2), then (0,4)
  over the empty (0,3), (0,5), then (2,5) over the empty (1,5), (3,5), and up
  to (4,2) when row 5 ran out. A test pins that path.

- **2026-10-26, live, unattended:** the in-game part loaded into the running
  game, found its functions, and attached to the camera update without a
  hitch. F8 dropped the camera behind the character and restored the game's
  own camera exactly. Through `--replay` of
  `tests/fixtures/traces/camera-turn.json`, GDPilot switched to third person
  from the right-stick click, turned the camera right to face Kerrick and back
  with the stick, and switched back, all through the pipe. The cursor moved to
  the aim point as third person began.

- **2026-10-27 and 2026-10-28, live, unattended:** every panel was reported as
  it opened and closed; GDPilot followed panels it never opened itself; the
  bags were read exactly, and the bag trace stopped once on the tall sword;
  collision pulled the camera in front of clutter; zoom, following and the
  skill wheel with real icons all worked from recorded pad traces.

- **2026-10-29, live, unattended:** in Devil's Crossing the in-game part found
  the foes beyond the town wall and, after the sight-line fix, offered none of
  them through it. A lock taken at the screen's edge brought the enemy to the
  middle in under a second without passing it. At Burial Hill, through
  `--replay` of `tests/fixtures/traces/lock-fight.json`, the right stick click
  locked onto a Walking Dead, the marker and the cursor sat on it, the held
  attack killed it, and the lock moved on to the next.

383 core tests and 66 integration tests, all passing.

Not yet verified:
- **Live pad movement.** The sticks rest at exactly 0.0000, so a firmware
  deadzone or a reading that never updates are both possible. Moving a stick
  during `--dry-run` settles it.
- **The pad's D mode.**
- **A real button press reaching the game end to end.** Each link is checked
  separately, but nobody has pressed the pad with the game running.
- **Rumble felt in the hand.** `--rumble-test` ran without error, but nobody
  was holding the pad; the same goes for lock-on's ticks.
- **Lock-on from the real pad.** Every lock so far came from recorded traces.
- **The pad-driven wizard in the game.** Its steps run live through `--at`;
  the stick-and-button loop around them needs someone at the pad.

## Known issues

- Without the in-game part, an item larger than one cell takes one press per
  cell, as the screen reading cannot tell items apart reliably. The stash is
  always read off the screen; its sacks are not yet reached from the game.
- Collision does not stop for trees and some props; the game fades those itself.
- With the panel closed but the tool still in a panel mode, the reading sees
  the world, which reads as all items, so presses step as they did in Phase 3.
  Harmless, and only after a mode desync, which Start and Back resolve.

## Blocked

Nothing.

## Human tasks

| Task | Phase | Status |
| --- | --- | --- |
| HT-1 Confirm the game accepts synthesized input | 0 | Passed 2026-10-12 |
| HT-2 Capture calibration fixtures | 3 | Done: bags and vendor 2026-10-20, stash 2026-10-24, unattended |
| HT-3 Feel tuning | 8 | Not yet raised |

## Next session starts here

Phases 6 and 7, PLAN.md section 17.

1. **Smart interact.** A nearby list like lock-on's, with loot, characters
   and chests: one button to pick up, talk or open the nearest.
2. **The stash from the game.** Find the stash's sacks, so the stash is exact
   too.
3. **Snapping in every menu**: skill tree nodes, devotion stars, dialogue
   choices, the blacksmith's list.
4. **Lock-on polish**, if play shows the need: a camera that recentres behind
   the character when there is nothing to lock, and pitch that frames tall
   enemies.

Workflow: `src\GDPilot.InGame\build.cmd`, `dotnet build`, restart the game,
enter the world, `GDPilot.exe --load-in-game`, then replay a trace. A new
in-game build always needs a game restart. The game driver used for unattended
checks lives in `%LOCALAPPDATA%\GDPilot\driver`, and a clone of GrimAction in
`%LOCALAPPDATA%\GDPilot\reference`, both outside the repository.

When someone is at the pad, also settle "Not yet verified": move the sticks
during `--dry-run`, try D mode, run `--calibrate` with the stick, and feel
`--rumble-test`.
