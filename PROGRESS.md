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
| 6. Lock-on, camera-relative movement, smart interact | Done: all checked live |
| 7. Radial skills, rumble, snapping in every menu | Done and checked live, but for three windows not yet reached and feeling it at the pad |
| 8. Shortcuts, profiles, first run, release | Done but for the feel session at the pad and publishing a release, both the owner's; see "Phase 8's end and hardening begun" |
| Souls-level feel (PLAN.md section 17) | Done, but for tuning with the owner at the pad |
| 9. Hardening | Begun 2026-11-08: soak, full trace regression, kill switch and focus mid-action, fuzzed reports, thread faults |

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

## Souls-level feel, 2026-10-30

| Piece | Status |
| --- | --- |
| Chest aiming | Done: three fifths of each enemy's own height, read live as 2.0 to 2.2 for the Walking Dead |
| Framing off the shoulder | Done: settles the locked enemy at 0.55 of the width, live |
| Recentring on an empty press | Done: swung the camera back behind the character, live |
| Gliding cursor and marker | Done, in Core, tested |
| Soft aim | Done, in Core, tested; not yet seen live, as the enemies nearby had died |
| Dodge on B | Done: right, left and ahead aimed through the game's ring of ground, live |
| Cost of the in-game part | Measured live: about 30 microseconds a frame; the switch's hitch cut from 1.8 to 0.4 ms |
| Overlay | Windows moved only when they move; kept on top once a second |

## Walking in any direction, 2026-10-31

| Piece | Status |
| --- | --- |
| Steering the game's walk | Done: thirty degrees right of forward walked at exactly that angle, live; back and left too |
| Keys kept as the trigger and the fallback | Done; without the in-game part, eight directions as before |
| Steering lapses without GDPilot | Done: half a second after the last direction |
| Fight rumble | Done in Core and tested; `--rumble-test` played it on the pad without error, not yet felt |

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

- **2026-10-30, live, unattended:** recentring brought the camera back behind
  the character; a locked enemy settled just right of the character; the dodge
  aimed right, left and ahead through the reported ground; and the in-game
  part's cost was measured at about 30 microseconds a frame.

- **2026-10-31, live, unattended:** a probe found the call the game walks
  through; with it turned, the character walked at the stick's exact angle in
  both directions tried.

- **2026-10-31, live, unattended:** A picked up an item dropped beside the
  character, talked to Kerrick and travelled through the rift gate, each with
  its prompt; twin-stick aiming, tilting and a framed lock all worked among
  the Burial Hill dead.

- **2026-11-01, live, unattended:** the restyled overlay showed in the
  game's own panel style, folded after five seconds, and sat clear of the
  inventory panel; no empty card flashed at startup after the fix.

487 core tests and 72 integration tests, all passing.

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
- **Soft aim among living enemies.** A dodge with Evade in slot 4 was checked
  live on 2026-11-08.
- **The pad-driven wizard in the game.** Its steps run live through `--at`;
  the stick-and-button loop around them needs someone at the pad.

## Known issues

- Without the in-game part, an item larger than one cell takes one press per
  cell, as the screen reading cannot tell items apart reliably. With it, the
  bags, the stash and a trader's goods all come from the game.
- The devotion map need not pan: fully zoomed out it all fits, and zoom is a stop.
  A few stars and a few points of artwork are still misjudged by the finder.
- A game window taller than the space above the taskbar, such as 1280x1024 on a
  1080 screen, has its foot under the taskbar, and a click on a hotbar slot there
  reaches the taskbar instead. Seen once, by the survey; no such window is usable
  anyway, as the taskbar covers the game's own hotbar.
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

## Combat checkpoint, 2026-10-31: complete

| Piece | Status |
| --- | --- |
| Twin-stick aiming | Done, tested; live, the reach was pulled in after it hit the screen's edge |
| Tilt on the right stick, LT to zoom | Done: checked live up and down after the second approach |
| Lock framing from the camera | Done: close enemies framed beside the character, live |
| Lock point every frame | Done, live |
| Smart interact | Done: talk, travel and pick-up all worked live from A, with the prompt |

## Other displays and Phase 8, 2026-11-07

Any resolution and UI scale now works from the one measured profile; nothing is
refused and nothing needs calibrating. How the game sizes its interface came from
its own code (`UiFactor`), what each part is anchored to from captures
(`UiAnchors`), and each reader takes the scale. `docs/calibration.md` has the
sums and the table.

Captured, every window that could be reached, at 1920x1080, 1600x900, 1366x768,
1280x720, 1024x768 and 1280x1024 at UI scale 0.5, and 1920x1080 at 0.75 and 1.0;
1280x720 at 1.0 and 1600x900 at 0.75 for the first batch of windows only. Every
reader run over those captures, with crops of the extremes kept as fixtures in
`ResizedDisplayFixtureTests`. Played live through `--replay` at 1280x720 in a
window off the screen's corner and at 1920x1080 at UI scale 1.0: bags, skills,
the devotion tab and stars, the map's rift gate, the menu, a conversation's reply.

Found along the way: a thin line drawn at any other size falls across two pixels
and loses up to half its contrast, which the frame lines and the slot list's edge
now allow for; and the devotion map is not scaled at all.

Also checked live: GDPilot started with no game, the game started after it, the
in-game part loading itself at the main menu and reporting `Playing Semitarded`;
the tray icon's menu opening, and Quit stopping everything cleanly; `--setup` from
the published package, on a first run and after it.

Not reached at other displays: the class screen, the Inventor, the Service windows
and the attributes' plus buttons, which take the middle-of-the-window anchor
unmeasured. Not possible on this machine: anything above 1920x1080, where the sums
come from the game's code but no capture has checked them; and ultrawide. The slot
list was not captured at 1024x768 or 1280x1024. Tray: Pause, Settings file, Log
folder and Start with Windows have not been clicked; the last has its own tests
against a key of its own.

## Phase 8's end and hardening begun, the night of 2026-11-08

Phase 8's own work is done. New and checked live tonight:

- **Rift travel on LT+LB**, through the game's Personal Riftgate key (L). From Burial
  Hill: the rift torn, A at it, the D-pad on the gates of the world map, A to
  travel. A personal rift and a dungeon's way in now read as travel.
- **The drop-in loader.** `--install-loader` puts a pass-through dinput8.dll in the
  game's x64 folder; the game loads it, it loads the in-game part once the game has
  settled, and GDPilot need not be running. Its first build took the game down:
  the game frees DirectInput more than once while starting, and the loader's
  thread lost its code. It pins itself now. Keys, replays and `--load-in-game`
  all checked with it in place.
- **Settings saved while playing** take hold at once; a broken save is refused with
  the reason and the last good settings stand.
- **The game's own keys** are checked against GDPilot's on a first run and by
  `--setup`, by each function's line in keybindings.txt, read off the game's own
  keybinding screen.
- **A carried item.** The bags' D-pad used to jump only between items, which left no
  way to the empty cells to put a picked-up item down. The game draws a carried item
  through CursorHandlerItemMove::Render, and while it does, the D-pad steps cell by
  cell: armour moved four cells along the second bag and put down, live.

Hardening, checked live:

- Every trace in `tests/fixtures/traces` replayed against the game, town and combat,
  with nothing held afterwards; `bag-exact.json` is a stored layout report, not a
  trace, and replaying it used to crash GDPilot, now refused with the reason.
- The kill switch in the middle of a walk and while the camera turned: keys let go,
  the camera stopped, both back on release. The taskbar taking focus mid-walk.
- No false main menu during a rift's loading screen.
- A 45 minute soak of every window, walking and turning: GDPilot level at about
  93 MB and 440 handles, no errors, 240 ticks a second against 250; the in-game part
  44 microseconds a frame, worst under a millisecond, no slow frame.

Hardening, by reading and tests:

- A report line with a value of the wrong kind could end GDPilot; it is now a bad
  line, and a fuzz test of 20,000 mangled lines holds it there.
- The overlay's and the tray's threads carry past a fault rather than end the run.
- The kill switch's hook is put in afresh every minute, since Windows drops a slow
  low-level hook without a word.
- One GDPilot at a time; an unknown option is refused by name rather than starting
  everything; the smoke test no longer writes a first run's settings into the
  person's own folder.
- A pad pulled out lets go of everything, and the card says so; one plugged back is
  followed, keeping its calibration if it is the same pad.
- The in-game part's slow-frame line now says how much of a frame was its own work
  and how much Windows took the thread away.

Also checked live: the game closed under a running GDPilot, which carried on and
connected again when the game came back; a dodge with Evade in slot 4, its
cooldown showing and the character dashing the stick's way; and the in-game part's
memory, flat over ten minutes connected beside ten alone, so the game's own growth
in the soak was its scenery, not GDPilot. GDPilot idle took a tenth of a core,
nearly all of it naming the window in front every tick; it now takes about 4.4 per
cent, the overlay half a per cent of that.

The loader was taken back out of the game's folder at the end of the night, so the
game is exactly as it was; `GDPilot.exe --install-loader` puts it back.

Lost tonight: one scroll from Semitarded's second bag, picked up to test the carried
item and still on the cursor when the game was closed. Nothing else was touched.

Not possible tonight: pulling the pad out for real, dying, and anything with someone
at the pad.

## Next session starts here

The combat checkpoint is complete. Next, PLAN.md section 17, Phase 7.

Phase 7's work is done, but for what cannot be reached or needs a person.
Everything in it was checked live by 2026-11-06, the Transfer stash, the main
menu's dialogs and a quest conversation played to its end last.

Left of Phase 7:

1. **Windows not met:** the Spirit Guide and the enchanter and potion crafters are
   not in Devil's Crossing. An unmeasured one reports as Service or Crafting, with
   no stops or the wrong ones, and the right stick's cursor still reaches all of it.
   Measure each the first time a character reaches it. Kory the Keeper was not found.
2. **With someone at the pad:** the "Not yet verified" list above, and tuning:
   the rumble, the real sticks and the speeds.

Next: Phase 9, hardening, PLAN.md section 17; the feel session with the owner at
the pad; and a release, whose publishing is the owner's. Selling needs no shortcut:
the game's own Quicksell answers X at a trader.

Only start Grim Dawn for a live check, and close it as soon as the check is
done.

Workflow: `src\GDPilot.InGame\build.cmd`, `dotnet build`, restart the game,
enter the world, `GDPilot.exe --load-in-game`, then replay a trace. A new
in-game build always needs a game restart. The game driver used for unattended
checks lives in `%LOCALAPPDATA%\GDPilot\driver`, and a clone of GrimAction in
`%LOCALAPPDATA%\GDPilot\reference`, both outside the repository.

When someone is at the pad, also settle "Not yet verified": move the sticks
during `--dry-run`, try D mode, run `--calibrate` with the stick, and feel
`--rumble-test`.
