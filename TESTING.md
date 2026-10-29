# Testing

Nearly all of this project verifies itself. Three tasks need a person, and they
are the last three sections of this file.

## The automated suite

From the repository root:

```
dotnet build
dotnet test
```

Both must pass before every commit. No test in the suite needs a gamepad, and
no test needs Grim Dawn running.

The loopback tests in `GDPilot.Integration.Tests` are the one place the suite
touches the real operating system. They launch a small window titled
"GDPilot Loopback", inject real key and mouse events, and assert what the
window's global hooks recorded. The window swallows injected events after
logging them, so nothing lands in other applications, and your own typing
passes through untouched. The window closes when the tests finish.

The pipe tests stand in for the in-game part on a pipe of their own, and the
loader tests load a system DLL into a hidden `ping` process; neither touches
the game. Close Grim Dawn before a full run anyway, since the loopback tests
inject input into whichever window is in front.

## Checking against the game

The in-game part is C++ and has no unit tests of its own; it is checked live,
in the game, with recorded pad traces:

1. `src\GDPilot.InGame\build.cmd`, then `dotnet build`.
2. Start Grim Dawn and enter the world. A new build of the in-game part needs a
   fresh game, because it is never unloaded from a running one.
3. `GDPilot.exe --load-in-game`. What it found, and every switch it makes, goes
   to `%LOCALAPPDATA%\GDPilot\in-game.log`.
4. `GDPilot.exe --replay tests/fixtures/traces/<trace>.json` plays a recorded
   pad performance through the whole program with the game in front.

| Trace | Checks |
| --- | --- |
| `camera-turn.json` | Switching to third person, turning both ways, switching back |
| `camera-zoom.json` | Zooming in and out with the right stick |
| `camera-follow.json` | The camera swinging round toward a sideways walk |
| `camera-wall.json` | Backing into clutter; the log shows collision pulling in |
| `bag-jumps.json` | Jumping between items in the second bag |
| `skill-wheel.json` | Holding the wheel, pointing at a slot, casting on release |
| `stash-tour.json` | Stash cells, tabs, every grid, and a split |
| `lock-on.json` | Locking on, flicking right then left, letting go; the log names each lock |
| `lock-fight.json` | Locking on and holding the attack, near enemies outside a town |

Lock-on needs foes near the character and in view. `GDPilot.exe --targets`
prints what the in-game part sees, which of them are in view, and which one a
press would lock onto; in a town the foes are usually beyond the walls, so it
reports none in view, which is correct. The low-level cemetery by the Lower
Crossing riftgate has enemies a few steps from the gate. The log records the
camera's measured turn as third person first starts each session.

What each trace sent in the game is pinned by a test, so a change that alters
it shows up in the suite, not only in the game.

## The three human tasks

These are the only three. Anything else that seems to need a person needs a
better fixture instead.

### HT-1 — Confirm the game accepts synthesized input

Phase 0. **Passed on 2026-10-12** on the scancode rung; the result is in
`PROGRESS.md`. Rerun it after a game patch. It takes about a minute and scores
itself.

Before starting, set the game up as section 4 of `PLAN.md` describes: Steam
controller configuration off for the game, in-game controller support off,
control scheme Keyboard Movement, display borderless windowed.

1. Load a character and stand somewhere open and quiet. Rain, fire, moving
   water and swaying trees all change the picture on their own, and the probe
   measures how much the picture changes. A still, indoor spot is ideal.
2. Leave the character standing still. Do not touch the mouse or keyboard once
   step 4 starts.
3. Alt-tab out of the game and run:

   ```
   dotnet run --project src/GDPilot.Spike -- --probe
   ```

4. It counts down from five. Alt-tab back into the game before it reaches zero.
   Whichever window is in front when it hits zero is the window it measures.
5. Wait. It runs for roughly fifteen seconds with the game in front, because
   injected input only reaches the focused window. It beeps when it is done.
6. Alt-tab back and read the verdict. It is also written to `SPIKE_RESULTS.md`
   next to the executable.

Report the single word after "Verdict": PASS, FAIL or INCONCLUSIVE.

Check that the "Target:" line names the game. If it names something else, the
alt-tab in step 4 did not land in time and the run means nothing.

INCONCLUSIVE means the movement could not be told apart from the scene's own
motion. Move somewhere stiller and run it again.

### HT-2 — Capture calibration fixtures

Done: the bags and vendor on 2026-10-20 and the stash on 2026-10-24, by the
development machine without a person. The captures are in
`tests/fixtures/screens`.

### HT-3 — Feel tuning

Phase 8. Not yet ready to run. Until then, every speed and the camera's view
are settings: `input` in `config.json` and `%LOCALAPPDATA%\GDPilot\camera.ini`.
