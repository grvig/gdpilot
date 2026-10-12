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

Phase 3. Not yet ready to run.

### HT-3 — Feel tuning

Phase 5. Not yet ready to run.
