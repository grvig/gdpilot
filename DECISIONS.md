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
