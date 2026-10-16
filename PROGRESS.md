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
| SDL binding chosen, `SdlGamepadSource` | Done with `ppy.SDL3-CS`; not yet read from a real pad, which was unplugged |
| `ReplayGamepadSource` and the JSON trace format | Done |
| Trace fixtures in `tests/fixtures/traces/` | Not started, arrive with the mapping work |
| Output intents, `IInputSink`, `RecordingSink` | Done |
| `SendInputSink`, scan codes by default | Done, loopback-tested |
| Kill switch, release-all, watchdog | Done: `GuardedSink`, `KillSwitchHook`, `Watchdog`, with `Suspension` keeping the kill switch sticky |
| Focus watcher | Done: `FocusWatcher` over `ForegroundProbe`; nothing runs it yet, that waits for the host |
| Resting stick calibration | Done |
| Radial deadzone with edge rescaling | Done |
| Left stick to eight-way WASD | Not started |
| Right stick cursor curve | Not started |
| Button table and chord layer | Not started |

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

107 automated tests, all passing.

Not yet verified: reading a real pad. On 2026-10-16 no controller was
connected. Windows has seen this pad before in both modes, as an "Xbox 360
Controller for Windows" and as a "HID-compliant game controller".

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

1. If a pad is connected, read it through `SdlGamepadSource`: name, resting
   centre through `StickCalibration`, and drift against the deadzone. Check
   both the X and D positions of its mode switch.
2. The mapping: left stick to eight-way WASD, the right stick cursor curve,
   the button table and the left-trigger chord layer, each driven by trace
   fixtures through `ReplayGamepadSource` into `RecordingSink`.
3. A host that wires these together: the safety layer, the focus watcher and
   the main loop. That ends Phase 1.

The Phase 0 leftovers are no longer needed after the scancode pass: the legacy
`keybd_event` rung, and the probe's check that its target stayed in front.
