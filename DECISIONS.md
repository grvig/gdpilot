# Decisions

Append an entry whenever something the plan left open gets resolved: a package
choice, a fallback taken, a constraint discovered on the machine. Newest last.

## 2026-09-12 — Installed the .NET 8 SDK

The plan assumes a .NET 8 SDK. The machine had no SDK of any version, so
`dotnet build` could not have run. Installed `Microsoft.DotNet.SDK.8` through
winget, which put 8.0.425 in `C:\Program Files\dotnet`. No change to the plan,
only to the machine.

## 2026-09-12 — Kept spike output untracked

Section 8 has the spike write its verdict to `SPIKE_RESULTS.md`. That file is a
per-run artefact tied to one machine, one resolution and one game build, and it
is rewritten on every run, so it is in `.gitignore`. The verdict that matters
gets copied into `PROGRESS.md` by hand, where it is a durable record rather than
a build output.

## 2026-09-12 — Put key identity in Core, injection in Output

Section 5 gives `GDPilot.Output` the SendInput wrappers and forbids `Core` from
referencing it. A binding table has to name keys, so the `KeyCode` enumeration
is pure data and lives in `Core`; only the P/Invoke that acts on it lives in
`Output`. The enum's numeric values are the Windows virtual key codes, which
lets the injector cast rather than translate.

## 2026-09-12 — Centralised build settings

Added `Directory.Build.props` at the root rather than repeating properties in
every project file. It turns on nullable reference types, implicit usings and
warnings as errors, and pins every project to x64. Warnings as errors is the
part worth keeping: this project is mostly interop, where a warning is usually
a real marshalling mistake.

## 2026-09-13 — Loopback swallows what it records

The loopback window's hooks log injected events and then block them from going
further. Without that, every test run would type and click into whatever window
had focus, typically the editor running the tests. Events not flagged as
injected are neither logged nor blocked, so the person at the machine keeps a
working keyboard and mouse while the suite runs.

## 2026-09-13 — Full project reference to the loopback executable

The integration tests first referenced `GDPilot.Loopback` with
`ReferenceOutputAssembly="false"`, only to force build order. That copies the
executable's launcher into the test output but not the assembly it launches, so
the window exited instantly with "application to execute does not exist" and
every test timed out. A plain project reference copies both. The fixture now
launches the executable from beside its own assembly.

## 2026-09-13 — One output layout for every project

Projects added while `Directory.Build.props` declared `<Platforms>x64</Platforms>`
were mapped to `Debug|x64` in the solution, and earlier ones to `Any CPU`, so
build output was split between `bin\Debug` and `bin\x64\Debug`. Removed the
`Platforms` property and mapped every project to `Any CPU`. `PlatformTarget`
still pins the binaries to x64, which is what the P/Invoke layer needs.

## 2026-09-19 — Per-monitor DPI awareness for anything sending absolute input

An absolute mouse move to x=200 landed at x=250 in the loopback test. This
machine runs 1920×1080 at 125% scaling, and a process that has not declared
DPI awareness is shown a virtual 1536×864 screen, so `GetSystemMetrics` and
`GetCursorPos` are off by the scale factor. Grid navigation clicks computed cell
centres, so it would have missed every cell by a quarter. Added
`DpiAwareness.EnablePerMonitor()` in `GDPilot.Output`. Every process that
reads or sends absolute coordinates calls it before creating a window: the test
fixture now, the spike and the app when they do.

## 2026-09-19 — One shared loopback window for all loopback tests

A low-level hook sees injected input from every process on the machine, not
only from its own test. xUnit runs test classes in parallel by default, so a
key test class and a mouse test class each with their own loopback window would
record each other's events. Both classes now belong to one xUnit collection
that owns a single loopback window, which also makes them run one after another.

## 2026-09-20 — The probe takes the foreground window

Section 8 forbids hardcoding the game's window title or executable name. The
probe counts down from five and takes whatever window is in front when it
reaches zero, so the person running it identifies the game by bringing it
forward. The report names the target it measured, which is also the check that
the run is meaningful: a target line naming something else means the alt-tab
missed and the scores describe the wrong window.

## 2026-09-20 — Capture from the screen, not the window

`ScreenCapture.Region` copies from the screen device context rather than the
game window's own. A game drawing through DirectX leaves its window context
empty, so a window capture would return blank frames and every probe would
score zero, which looks identical to input being ignored. The composited screen
always holds the pixels. `CAPTUREBLT` is set so a layered window over the
region is included rather than skipped.

## 2026-09-20 — The ladder automates two rungs, not four

`FeasibilityLadder` climbs the two rungs that are a code change: virtual key
codes, then scan codes. An inconclusive rung is retried once with a doubled
hold before moving on. The remaining rungs in section 8 are not code: running
the game windowed rather than borderless, and running elevated. Both are
printed as guidance when every automated rung fails. The legacy `keybd_event`
rung is not built yet.

## 2026-09-23 — Positive Y is up in every stick value

Hardware disagrees about stick Y: SDL reports down as positive, XInput reports
up as positive. `StickPosition` fixes up as positive, the maths convention, so
angles from `Atan2` mean what they look like and the movement and cursor maths
never need to know which pad produced the value. Flipping is the job of the
source adapter, once.

## 2026-09-23 — Deadzone rescaling assumes the worst-case drift

A drifted centre makes the throw shorter on the side the stick leans towards.
Rescaling uses the whole drift magnitude as lost travel, in every direction. A
full push towards the drift still reaches magnitude 1, but a push away from it
saturates a little early. Per-direction reach would be exact but needs the
stick's real gate shape, which calibration does not measure. Early saturation on
one side is invisible in play. Never reaching full speed on one side is not.

## 2026-09-23 — Capture test waits for the first paint

The capture test failed once on a cold rebuild and passed on every rerun. The
loopback window writes "ready" when it is shown, which is before its first
paint reaches the screen, and a cold start widens that gap enough to capture
whatever was underneath. The test now retries the capture for up to two
seconds until the window's colour appears. It still fails if the colour never
appears, so it still catches a broken capture.

## 2026-10-03 — Calibration refuses rather than guesses

Section 9 asks for the mean of 500 ms of resting samples. A mean alone cannot
tell a resting stick from one a thumb is leaning on, and accepting the leaning
one would make that push the new rest, so the character walks on its own once
the thumb lets go. `TryFindCentre` refuses a mean beyond 0.3 from true centre,
or any sample more than 0.08 from the mean, and the caller keeps its previous
centre. Both limits sit well clear of a worn budget stick's real drift.

## 2026-10-03 — Traces name their buttons, replay takes its clock

The plan's trace format holds a button bitfield. Fixtures are written by hand,
and `"buttons": 17` says nothing to a reader, so the parser also accepts names
such as `"A, LeftBumper"` through the standard JSON enum converter. Axes a
fixture omits are at rest. `ReplayGamepadSource` reads its time from a function
the caller supplies rather than from a stopwatch, so a test steps through a
trace exactly and the same trace always yields the same snapshots.

## 2026-10-10 — Intents are records, and released input is tracked centrally

Section 6 names `IInputSink` and `RecordingSink` without saying what an intent
is. Intents are C# records (`KeyDown`, `KeyUp`, `PointerMove`, `PointerMoveTo`,
`PointerDown`, `PointerUp`), so a test compares a whole output sequence with one
equality check. Held-input tracking lives in one decorator, `GuardedSink`,
rather than in each caller, because the release path has to run from the kill
switch, the watchdog, exceptions and process exit, and all of those must agree
on what is held.

## 2026-10-10 — The capture test reports instead of guessing

Two guessed causes for the flaky capture test, a slow first paint and the
window cascading off-screen, were each disproved. Rather than guess a third,
the test now names the window that owns the pixel at the capture's centre when
it fails. The first failure after that change named the cause: another
application's window drawn over the loopback window.

## 2026-10-12 — Scan codes are the only key mode that moves the character

HT-1 passed on the scancode rung. Virtual key events moved nothing, scoring
5.81 against an idle 5.77. `SendInputSink` therefore defaults to scan codes. The
human tasks were run unattended through desktop automation at the owner's
request, and HT-2 will be too. HT-3 cannot be: it needs hands on a pad.

To make the probe meaningful, the game's own options changed:
- Window mode: borderless.
- Movement type: Keyboard.
- Evade: no longer follows the cursor.
- Controller support: off.
- Forward: bound to W. That unbound Switch Weapons, which used to be on W.
  Backward, Left and Right are still unbound, and S still opens the skill
  window.

The original settings file is kept outside the repository.

## 2026-10-12 — The kill switch fires on extra modifiers

`KeyChord.Matches` accepts a held Shift on top of Ctrl+Alt+Pause. For a kill
switch, firing by accident costs a re-enable, and refusing to fire costs stuck
keys. `StallDetector` reports a stall once rather than on every watchdog pass,
so a long stall releases held keys once, not hundreds of times.

## 2026-10-13 — The kill switch hook decides nothing itself

All of the kill switch's judgement lives in `ChordWatcher` in `GDPilot.Core`:
modifier tracking, and reading Cancel as Pause, since Windows renames Pause to
Cancel while Ctrl is held. `KillSwitchHook` only owns a thread, a message loop
and the hook, so the decision logic is unit-tested without touching the
operating system. The hook has its own thread because a kill switch that
shares the main loop dies when that loop stalls. The callback runs on the
thread pool, because Windows silently removes a low-level hook that is slow to
return. The hook swallows nothing, and it accepts injected events. That lets a
loopback test press the chord, and it lets another remapping tool trigger the
kill switch too.

## 2026-10-15 — Suspension has reasons, and the watchdog outlives its failures

Section 7 has one `Suspended` state, entered both by the kill switch and by the
game losing focus. With a single flag, alt-tabbing back would quietly undo a
kill switch pressed on purpose. `Suspension` records each reason separately and
re-enables injection only once all have cleared. The `Watchdog` catches and
records a failing release rather than letting it end the thread, because a
watchdog that dies on its first failure leaves the next stall unwatched. Focus
is polled rather than hooked, because a poll cannot miss a transition.

## 2026-10-16 — SDL3 through ppy.SDL3-CS

Section 9 asks for an SDL binding for .NET that restores cleanly. Candidates:

| Package | Downloads | Notes |
| --- | --- | --- |
| `ppy.SDL3-CS` | 610k | Chosen |
| `ppy.SDL2-CS` | 2.1M | SDL2, now in maintenance |
| `Silk.NET.SDL` | 518k | SDL2 |
| `SDL3-CS` + `SDL3-CS.Native` | 93k | Smaller, single maintainer |

Chose `ppy.SDL3-CS` 2026.1002.1. It is MIT licensed, so it is compatible with
this project. It is actively maintained, ships `SDL3.dll` for win-x64 inside
the package, and restored at the first attempt. No code was copied from it; it
is a package dependency. SDL's own licence is zlib.

`SdlGamepadSource` sets the joystick background events hint. GDPilot never owns
the focused window, and without the hint SDL stops reporting input once another
window has focus. It reopens the first pad whenever none is connected, so a pad
plugged in later, or flipped between its X and D modes, is picked up without a
restart.

## 2026-10-17 — Mappings describe held state; one differ emits the changes

Every mapping answers one question: what should be held right now. That covers
stick directions, button layers, and pointer buttons. `HeldInputDiffer` is the
only code that compares this tick with the last and emits presses and releases.
That keeps each mapping stateless and testable as a plain function. It also
gives one place to order events, releases before presses, so swinging the stick
from up-left to up-right never holds A, W and D at once.

While the left trigger is held, the chord layer replaces the base layer
outright rather than falling through to it, as section 9 describes a second
table. The cursor carries the leftover fraction of a pixel into the next tick,
because a gentle push at 250 Hz asks for under a pixel per tick and would never
move otherwise. The leftover is dropped on release, so the next push starts
clean.

## 2026-10-18 — Configuration additions and what it refuses

Section 7's configuration gains these keys, each with a default, so a file only
needs to state what it changes:

| Key | Default | Purpose |
| --- | --- | --- |
| `gameProcess` | `Grim Dawn` | Process name the focus watcher checks for |
| `input.walkThreshold` | 0.25 | How far the shaped left stick must lean before movement keys are held |
| `input.chordTrigger` | 0.5 | How far the left trigger must pull to switch to the chord layer |

Bad values fail at load time with a message naming the entry, rather than at
the first tick. Unused sections, such as `profiles`, are read past. `grid:`
actions are refused until Phase 3 gives them a meaning. A key binding that
writes a modifier, such as `key:Ctrl+Space`, is refused: a binding holds one
key, and dropping the modifier silently would make it do something other than
what the file says. Bare digits mean number keys, as in the plan's own
`"LT+A": "key:1"`.

## 2026-10-18 — Commit size relaxed for this project

The owner raised the commit size guide from 40–90 lines to about 200, treated
as a soft limit, so a feature and its tests can share a commit. The other
commit rules are unchanged. `PLAN.md` keeps its original wording, since it is
the verbatim brief, and this entry is the record of the change.

## 2026-10-19 — Host behaviour

- **Kill switch toggles.** The first press suspends; the next press resumes.
  The chord watcher ignores key repeat, or holding the chord would flicker it.
- **Held input is pressed again after a suspension.** The guarded sink has
  already released everything, so the control loop resets the mapper while
  suspended. A stick still held on return presses its keys again rather than
  the mapper believing they never stopped.
- **The host asks for 1 ms timer resolution.** Windows sleeps in 15.6 ms steps
  by default, which would cap the configured 250 Hz at about 64 Hz. The host
  measured 251 Hz on this machine.
- **A dry run skips the focus check.** That way it shows output with the game
  closed.
- **Configuration is found in order:** `config.json` in the working
  directory, then beside the executable, then the shipped
  `config.example.json`, then built-in defaults.

## 2026-10-19 — Modes follow the game's own panel behaviour

**Panel keys.** Grim Dawn's panel keys toggle, so `ModeStack.Toggle` closes a
panel that is already open. If that panel sits lower in the stack, it is
removed from where it sits, so the stack never holds a panel twice. Stash,
Vendor and Dialog have no key, because the game opens them by clicking a
character or object; binding one of them only records that it is open.

**Back.** `back` sends Escape and pops one level. From Combat it pushes Menu,
since that is what Escape does in the game.

**Taps.** Mode bindings act once per press, and their keys are held 40 ms. A
press and release in the same instant can fall between two game frames.

**Panic resync.** Start and Back held for half a second sends one Escape per
tracked panel and then clears to Combat, as section 10 describes. It cannot
help when the tracked depth is lower than what is really open. Whether to send
extra Escapes waits on seeing how the game treats Escape with several panels
open.

**Modes without a section.** A mode with no binding section uses Combat's.
Otherwise opening a panel bound to nothing would leave no button to back out
with.

## 2026-10-19 — The overlay owns its thread and places itself in pixels

The overlay runs its own WPF dispatcher thread, so a slow redraw can never
delay the input loop, and the input loop never has to be a UI thread. WPF can
make a window see-through but not click-through. The extended styles
`WS_EX_TRANSPARENT`, `WS_EX_NOACTIVATE` and `WS_EX_LAYERED` do that, and a test
reads them back from the live window.

The overlay is positioned through `SetWindowPos` in physical pixels, not WPF's
`Left` and `Top`, which are in scaled units while the game's bounds are
physical. Topmost is re-asserted on every update. The card was raised to about
86% opacity after a capture showed text from the window underneath bleeding
through at 67%.

## 2026-10-19 — What the live check changed

Character is no longer a default panel. The game shows the character sheet and
the inventory in one window, bound to both C and I, so tracking them as two
modes would desync as soon as one key closed the window the other had opened.
Skills defaults to K, because keyboard movement needs S for walking backward.
Taps stay at 40 ms, which opened and closed the inventory in the game.

## 2026-10-20 — Grids belong to modes, and are checked once at startup

Grids are named after the mode they serve: `Inventory`, then
`Inventory.Bag2` for a second surface in the same mode. That is because the
same bag is drawn in different places depending on what else is open: with
the vendor's trade window up, the inventory panel sits 172 pixels further
right, at the same height, than when it is open alone. A grid per surface
would have clicked the wrong pixels in Vendor mode. (This entry first said
further left and higher. Measuring the vendor capture corrected it.)

`GridReadiness` compares the active profile with the live window size and the
UI scale read from the game's `options.txt`, once at startup, as section 11
asks. The game's settings file is only ever read.

The first grid press after entering a mode lands on the top-left cell rather
than stepping from wherever the cursor happened to be.

The fixture captures are full-frame lossless PNGs, about 4 MB each. That is
heavy for a repository, but the full frame keeps panel positions, which is
how the vendor shift was noticed.

## 2026-10-22 — Detection, the wizard, tabs and modifier clicks

Grid detection reads brightness profiles along rows and columns and looks for
an evenly spaced run of local peaks, walked out in both directions with 2.5
pixels of tolerance. A run counts only if at least three quarters of its
positions have a peak. Item art and half-cell artefacts add peaks between
lines, which first made the detector report a 16 pixel pitch. So when
neighbouring peaks alternate in strength across most of the run, the spacing
is doubled. A mean profile is used; a median was tried and did worse.

The wizard trusts a detected grid only when its first and last cell centres
sit within half a cell of the two points the person chose. It tries search
margins of 20, 32, 48 and 64 pixels in turn, because the right margin grows
with the UI scale, and a margin that is too wide takes in the panel border and
finds an extra row. When nothing agrees, the counts are set by hand and pitch
comes from the corners, as section 11 describes.

A stash tab strip is a one-row grid named `Stash.Tabs`. Treating it as a grid
reuses calibration, profiles and the readiness check. `grid:next` skips any
`.Tabs` grid, and the bumpers click through it, wrapping at the ends.

A modified click such as `mouse:ctrl+left` presses the key before the button
and releases the button before the key. The reverse release order would let
the game see a bare click at the end of the press.

Calibrations are saved by parsing the configuration and writing it back,
which loses comments. Keeping them would need a hand-written JSON editor, and
a lost comment is a smaller cost than a corrupted configuration.

## 2026-10-24 — The stash, its tabs, and checking without a pad

The stash's tabs are a column of buttons down its left edge, so a tab strip
may be one column as well as one row. The navigator steps along whichever
way the strip runs.

Below the owned tabs, the stash shows a coin button and padlocks, and clicking
either offers to sell another tab for iron bits. One click during calibration
bought a second tab, for 25,000 iron bits of a test character's money. So a tab
strip covers only owned tabs, and the shipped one has two. Wrapping at the
ends, rather than stepping past them, is what keeps the purchase prompt out of
reach.

`--replay` plays a recorded pad trace through the real host, injection and
overlay included, in place of the pad. It was added to check the whole chain
in the game with nobody at the pad, and it is how the stash was checked end to
end. The same trace is a test, so what was seen in the game stays pinned.

The wizard takes `--at` and `--counts` for the same reason, and for setups
where pointing with a stick is awkward. Both routes run the same wizard steps.

Right click uses or equips the item under it. It does not move items into the
stash, so no transfer binding is shipped.

## 2026-10-25 — How occupancy is read

The plan stores an empty-slot colour at calibration time. The captures made
that unnecessary: every empty cell, in every panel, is near-black and
colourless in its middle, so a fixed pair of thresholds on brightness and
colour separates empty from occupied with room to spare. Nothing extra is
stored, and an existing calibration gains jumping without being redone.

Between the two thresholds a cell is unsure rather than guessed. More than a
tenth of a grid unsure doubts the whole reading, and that press steps instead,
as the plan's "degrade, never freeze" asks.

The plan polls at about 8 Hz. Occupancy is read at the moment of a grid press
instead, for only that grid's rectangle. A press needs the screen as it is
then, a poll would spend captures while nobody is pressing, and nothing runs
outside a panel mode.

Cells hidden by a tooltip or the carried item keep their last trusted state
unless they now read as occupied. A hidden cell is recognised by its grid
lines not showing, since a visible line stands 46 to 96 brighter than its
surroundings and a covered one 3 to 5.

Grouping a multi-cell item into one target is not done yet. Neither grid lines
nor box edges mark item boundaries reliably in the captures: common items have
no box, and some boxes let lines show through.

## 2026-10-25 — A controller-first overhaul, with an in-game part

The owner widened GDPilot from a controller layer to a controller-first
overhaul: a third-person action camera, lock-on, radial skills and snapping
menus. A camera cannot be moved with key presses, so the founding rule against
touching the game process is amended, not dropped: an optional in-game part
may load into the game to drive the camera and read game state. It never
changes anything that affects how the game plays out.

Why it is feasible, checked on the installed game that day:
- The game's own DLLs export their functions by name. Game.dll names 25,113,
  including `GameCamera::SetZoom`, `WorldCamera::SetCameraPitch` and
  `InventorySack::GetInventory`, which returns every item's rectangle.
- GrimAction, an MIT-licensed third-person camera from September 2026, already
  maps the camera. Its pinned build no longer matches the installed game, but
  all 29 camera functions it uses are still present by name; only their
  addresses moved, and in five, a field offset shifted.

Why a hybrid rather than all of it inside the game:
- The kill switch must work when the game hangs, so it stays outside.
- A patch that breaks the in-game part must not take the controls with it.
  Outside, input has never depended on game internals.
- GrimAction found that reading the pad from inside the game broke the game's
  own controller input.
The user still sees one thing: one download, and the controller side loads
the in-game part itself. A drop-in loader in the game folder replaces that for
the public release.

The two parts talk over a named pipe restricted to the current user, in
one-line JSON reports carrying a protocol number. A mismatched pair says to
update both, once, instead of misreading. Reports the controller side does not
know are skipped, so either side can grow first.

Multi-cell items are taken from the game's rectangles, not the screen. Colour
along the shared edge of two cells separated items well in four of five grids,
but merged two same-coloured neighbours in the crowded main bag, and a merge
makes an item unreachable. The screen reading stays as the fallback, with every
cell its own stop.

Pressing a direction from an item now measures from that item's far edge, not
its top-left cell, so a press down from a tall sword goes below the sword
rather than to the item beside it.

`game-functions.txt` lists every function the in-game part may call, and
`--check-game` reads the installed DLLs' export tables to confirm them. All 33
were present on 2026-10-25.

Gyro aiming was considered and shelved by the owner.

## 2026-10-26 — The in-game part and the third-person camera

**Reuse from GrimAction (MIT, github.com/Ophemist/GrimAction, commit b8c2f4c).**
No source file was copied. What was taken is knowledge, credited in
`InGame.cpp`, `docs/camera.md` and the README:
- the camera object's field offsets: distance and pitch limits and defaults at
  0x10c, 0x110 and 0x570 to 0x57c, the live and target zoom blends at 0x580 and
  0x584, the zoom endpoints at 0x590 and 0x594, and the field of view at 0x14;
- the write order: set every field, call the game's `SetZoom`, set them again;
- the starting profile values, later tuned;
- attaching to `GameCamera::UpdateFromInputImpl` with Microsoft Detours, and
  loading by `CreateRemoteThread` on `LoadLibraryW`.
GrimAction is about 26,000 lines with telemetry, ownership overlays and a
launcher; the in-game part is a few hundred, since GDPilot keeps the pad,
the safety layer and the logic outside the game.

**Lookup by name, checked by bytes.** Functions are found with
`GetProcAddress` on their exported names. Each one's first bytes are compared
with the bytes it was mapped from, and those bytes contain the offsets of the
fields written, so a match is the evidence the layout is unchanged. On the
installed build the camera functions match although their addresses moved.

**Two zooms.** The first attempt set only the live zoom, and the game eased it
straight back to its target, which under the new limits put the camera inside
the character. Both blends are now set, through `SetZoom`.

**GrimAction's starting distance of 42 is a wide high view on this build**,
where the game's own camera sits at 20. The shipped defaults start at 10, from
4 to 30, with pitch from 12 to 40 degrees, read from `camera.ini` at every
switch so the view can be tuned in a running game.

**The pad never reaches the game.** GDPilot turns the right stick into look
rates and sends them down the pipe only when they change; the in-game part
applies them through the game's own `AdjustYaw` on the game's thread. Grim
Dawn's keyboard movement is relative to the camera, so turning the camera
turns "forward" with it, which is the camera-relative movement Phase 6 asked
for, at no extra cost.

**The pipe is buffered.** A Windows pipe without a buffer makes every write
wait for a read, which a test caught as a hang; in play it would have frozen
the control loop whenever the game was busy. The in-game side uses overlapped
I/O for the same reason: the game's thread only ever sets an event.

**A suspension stops the camera.** The kill switch stops every intent,
including the command that stops a turn, so the host sends a stop directly as
a suspension begins.

**F8 and a pad binding both switch.** F8 is read only while the game is in
front, and needs no GDPilot; `camera:toggle` is the pad's way, through the
pipe. The example binds it to the right-stick click, which was free.

## 2026-10-27 and 2026-10-28 — Reports from the game, collision, the wheel and rumble

**GrimAction 0.3.0 matches the installed build.** Its newest release names the
exact Game.dll and Engine.dll fingerprints installed here, so the offsets it
maps beyond the camera were taken as mapped on this build: the UI object at
`gGameEngine + 0x19b0` with one flag byte per panel, the player's controller id
at `Player + 0x16c0`, and the unexported object lookup at `Game.dll + 0x19d40`,
which is only called after its first bytes are checked.

**Panels come from the game.** The tracked mode follows the reported panels,
once any tap has had 300 ms to land, because a report is up to a tenth of a
second old. The game raises its inventory flag for the stash and a trader too,
so those modes are kept while it is up.

**Items come from the game.** Each bag is read through `GetInventoryInSack` as
a standard ordered tree, mapped by a logged probe on 2026-10-28: the head's left
link is the first item, each node holds the item id at 0x1c and its rectangle
at 0x20, in units of the game's inventory cell. Grid sizes and the cell size
come from the game's own accessors. The stash's sacks are not reached this way,
so the stash keeps screen reading.

**Collision starts its ray past the character.** The level query with surface
kind 0 meets terrain and buildings only, and every other kind met the character
itself first, even with the character named as the entity to ignore. Starting
the ray 1.5 units out along the arm and using kind 1 meets the town's carts and
clutter, and was seen pulling the camera in live. Thirty refused queries turn
collision off for the session.

**The right stick's up and down zooms rather than tilts.** The game tilts its
camera with its zoom, so zooming gives both, through the game's own easing of
its zoom target, with nothing extra to keep in step.

**Following is the host's.** Turning the camera toward a sideways walk needs
only the sticks, so it is a pure rule in the mapper sending ordinary look
rates. It never acts on a straight or backward walk, and the right stick always
overrides it.

**The wheel lives in the mapper.** Held, with a cast on letting go, it needs the
held state and the stick together, which the mode controller's press edges do
not see. Its icons are cut from one screen capture of the calibrated hotbar as
it opens; the hotbar was measured by hand, since its separate boxes give the
detector no ruled lines.

**Rumble is decided in Core from health alone**: a jolt for ten points lost
between two reports, and a heartbeat below 30% that quickens below 15%. Health
is read through `Character::GetCurrentLife` and `CharacterBio::GetLifeLimit`.

## 2026-10-29 — Lock-on

**Lock-on is on the right stick click, as in action games.** Switching the
camera moved to the left trigger with the click: the camera is chosen once, a
lock many times a fight.

**Targets come from the game's own query.** `GameEngine::GetTargetsInRadius`,
the call its skills use, already drops townsfolk, the dead and the hidden. Its
flag after the ignored id picks friends when true and foes when false: its
filter calls `FactionManager::IsFriend` for true and `IsFoe` for false, and a
live run in Devil's Crossing returned the town guards for true. The vector it
fills is kept for the session and only emptied, so the game's allocator alone
grows or frees it. Each id is checked to be a `Character` and not an `Npc` by
the class chain the game's own filter walks.

**Positions are drawn through the game's camera.** `WorldCamera::Project`, with
a viewport of four integers exactly as its constructor writes them, gives
pixels; checked live against a guard at the reported spot. Up is the y axis,
as the camera's offset shows. An enemy only counts as in view when it is in
front of the camera, inside the window, and the level query finds no wall
between it and the player. That ray starts 1.5 units out, as collision's does,
after the first version met the player's own body and let a lock through the
town wall.

**Choosing and switching are Core's.** The in-game part reports and turns the
camera; which enemy, the flicks, the falls and the breaks are all decided in
`LockOn` from the reports alone, and tested without a game. The first lock goes
to the enemy nearest the aim point, with a third of a screen height of weight
for distance across the whole reach. A flick picks the nearest enemy within
sixty degrees of its direction, with those off the line counting as further.

**The camera's turn is measured, then compensated.** The game eases the view
toward the yaw field over about a quarter of a second, so turning by the
remaining error each frame overshot by a fifth of the screen, seen live. The
in-game part now makes one small turn as third person first starts, measures
the direction and the easing time from the view's heading, and counts the turn
still on its way when steering. Live, a lock from the edge settled in the
middle without passing it.

**Aim at the chest, from the game's own height.** `Actor::GetHeight` is a
two-field product, checked by its bytes; the Walking Dead read 2.0 to 2.2
units tall live. Lock-on aims at three fifths of that, and falls back to
`lock_aim_height` when a height looks wrong.

**Recentring reads the character's own facing.** The rotation sits at 0xf0 in
the character, three axes of three floats, and the third is forward: checked
live by swinging the camera away and recentring, which left the character's
back to the camera.

**A lock is framed off the shoulder.** Steering straight at the enemy put it
directly behind the character, where the marker sat on the character's
shoulder. The camera now aims six degrees to one side, measured live to settle
the enemy at 0.55 of the screen's width, clear of the character.

**Between reports the cursor glides.** Reports come thirty times a second, so
the cursor and marker now carry on at the enemy's last speed for at most one
report's gap, rather than stepping.

**Soft aim is a pull, not a lock.** It reaches 0.14 of a screen height from the
aim point and 12 world units from the character, keeps its choice out to half
as far again, and shows no marker, so it never feels like a lock the player
did not ask for.

**The overlay moves only what moved.** Re-asserting topmost on every redraw
made the desktop recompose fifty times a second while the marker was up; now
a window is moved only when its spot changes, and kept on top once a second.
The in-game part was measured at the same time at about 30 microseconds a
frame, so it was never the cause of the dips the owner saw.

## 2026-10-30 — The dodge, and a bug pass

**A dodge is a movement skill aimed by the stick.** Grim Dawn has no dodge of
its own, and its movement skills go toward the cursor. B puts the cursor on
the ground the left stick points at, a few steps out, taps the skill, and
gives the cursor back once the key is up; nothing else moves the cursor in
between, or the skill would follow it. Slot 4 by default, so it is the owner's
choice which skill dodges.

**The ground comes from the game.** A point straight up the screen is the sky
in a low third-person camera, so the in-game part reports where the feet and
eight points of ground round them fall on the screen, and the dodge takes the
point lying the stick's way from the feet. Live, right, left and ahead landed
at the screen's right edge, its left edge and just ahead of the character.

**The bug pass found three.** A dodge did nothing before the game's window was
found; the marker and the wheel stayed drawn over other windows when the game
was not in front; and a lock outlived the character's death. Each is fixed,
the first two with tests. Switching third person on also read `camera.ini` a
dozen times over, a 1.8 ms frame; it reads the file once now, 0.4 ms.
