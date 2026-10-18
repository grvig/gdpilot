# Progress

Read this first in every session, then the plan section for the current phase.

## Current phase

Phase 1 — input plumbing and safety. Phase 0 is complete: HT-1 passed.

## State

| Phase | Status |
| --- | --- |
| 0. Feasibility probe | Done, HT-1 passed on the scancode rung |
| 1. Input plumbing and safety | In progress |
| 2. Mode state machine and overlay | Not started |
| 3. Grid navigation by calibration | Not started |
| 4. Occupancy detection | Not started |
| 5. Macros and release | Not started |

## HT-1 result, 2026-10-12

| Rung | Idle score | Moved score | Outcome |
| --- | --- | --- | --- |
| virtual key | 5.77 | 5.81 | Fail |
| scancode | 5.53 | 42.40 | Pass |

Grim Dawn v1.3.1.1 x64, borderless 1920×1080, Keyboard movement, standing in
Devil's Crossing. The character was seen to walk. Gameplay reads scan codes
only, while the options menu accepted a virtual key W, so menus and play read
input through different paths.

## Phase 1 checklist

| Task | Status |
| --- | --- |
| Gamepad snapshot and `IGamepadSource` | Done |
| SDL binding chosen, `SdlGamepadSource` | Done with `ppy.SDL3-CS`; reads the real pad, live movement unconfirmed |
| `ReplayGamepadSource` and the JSON trace format | Done |
| Trace fixtures in `tests/fixtures/traces/` | Done: walking and the chord layer, replayed end to end |
| `Mapper`, one tick of the whole mapping | Done |
| Configuration and binding tables | Done: `GDPilotConfig`, `BindingTable`, tracked `config.example.json` |
| Output intents, `IInputSink`, `RecordingSink` | Done |
| `SendInputSink`, scan codes by default | Done, loopback-tested |
| Kill switch, release-all, watchdog | Done: `GuardedSink`, `KillSwitchHook`, `Watchdog`, with `Suspension` keeping the kill switch sticky |
| Focus watcher | Done: `FocusWatcher` over `ForegroundProbe`; nothing runs it yet, that waits for the host |
| Resting stick calibration | Done |
| Radial deadzone with edge rescaling | Done |
| Held input to presses and releases | Done: `HeldInputDiffer`, releases before presses |
| Left stick to eight-way WASD | Done: `StickDirections` |
| Right stick cursor curve | Done: `CursorMotion`, with sub-pixel carry |
| Button table and chord layer | Done: `ButtonLayers` |

## Verified

- The solution builds clean with warnings as errors on .NET 8.0.425.
- Key and mouse injection reach the operating system, in both key modes.
- Grim Dawn moves the character on injected scan codes.
- `GuardedSink.Disable` releases a held key at operating system level.
- The kill switch hook fires on an injected Ctrl+Alt+Pause and stays quiet
  when Alt is missing.
- Absolute coordinates are correct on this 125% scaled display.
- Screen capture agrees pixel for pixel with an independent GDI+ capture.
- Deadzone, calibration, trace parsing, replay, chord parsing and stall
  detection are unit-tested.

- A stalled loop triggers the watchdog, which survives a failing release.
- Regaining focus does not undo a kill switch.

- SDL's native library loads and starts on this machine, and reads at rest
  with no pad attached.
- The real pad reads through `SdlGamepadSource`. On 2026-10-17
  `GDPilot.Spike --pad` found "Xbox 360 Controller", which means X mode.
  Both sticks rested at exactly 0.0000 with no jitter over 108 samples, so
  they fit both default deadzones.
- Eight-way direction sectors, cursor speed curve and sub-pixel carry, chord
  layer switching, and press and release ordering are unit-tested.

- Scripted pad traces replay through the real mapper into the exact
  expected key sequence: an eight-way walk, and the left-trigger chord layer.
- The configuration loads with defaults, rejects bad values at load time, and
  the tracked example file parses.

162 automated tests, all passing.

Not yet verified:
- **Live pad readings.** Exact zeros at rest usually mean the pad's firmware
  applies its own deadzone, which is common on budget XInput pads. A reading
  that never updated would look the same. Proving the readings are live
  needs someone to move a stick while `--pad` runs.
- **The pad's D mode.** It was in X mode.

## Known issues

None open. The flaky capture test is explained and fixed: another
application's always-on-top window covered the loopback window during runs.
The test now compares its capture with an independent GDI+ capture of the same
region, so it no longer depends on what is on screen.

## Blocked

Nothing.

## Human tasks

| Task | Phase | Status |
| --- | --- | --- |
| HT-1 Confirm the game accepts synthesized input | 0 | Passed 2026-10-12 |
| HT-2 Capture calibration fixtures | 3 | Not yet raised |
| HT-3 Feel tuning | 5 | Not yet raised |

## Next session starts here

1. `GDPilot.App`, the host. It loads `config.json`, falling back to the
   example, and calibrates both sticks at startup. Each tick it polls the pad,
   beats the stall detector, polls focus, and sends the mapper's output through
   `GuardedSink`, with the kill switch and the watchdog running alongside.
   Every exit path releases held input.
2. A `--dry-run` mode that prints intents instead of injecting them. With the
   pad plugged in, it shows the whole chain live without touching the game.
3. In-game keybindings before playing for real. Only Forward is bound, to W.
   Backward, Left and Right need S, A and D, and S currently opens the Skill
   window, which would have to move.

That ends Phase 1.

The Phase 0 leftovers are no longer needed after the scancode pass: the legacy
`keybd_event` rung, and the probe's check that its target stayed in front.
