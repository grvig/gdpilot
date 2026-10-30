# Project Plan

GDPilot — an external controller layer for Grim Dawn v1.3 (Fangs of Asterkarn).

This document is the single source of truth. Work through it in order.

**Expanded on 2026-10-25.** The owner widened the project from a controller
layer to a controller-first overhaul, with an optional in-game part for the
camera and for game data. Sections 1 to 16 are the original plan, amended
where marked. Section 17 holds the new phases, and supersedes section 13.

---

## 1. What this is

Grim Dawn ships native gamepad support, but its menus are a free-floating cursor
over a UI built for a mouse. Inventory and stash management are slow, the button
count forces awkward compromises, and traversal is cursor-steered.

GDPilot is a Windows background application that sits between the gamepad and
the game. It reads the controller directly, tracks which part of the game UI is
currently open, and synthesizes keyboard and mouse input accordingly. The
binding layout changes with the open panel, so the same stick aims in combat and
hops item to item in the stash. The game itself is never modified.

### Non-goals

Permanent. Do not add them.

- No game files are modified. No `.dbr` records, no `.arc` archives, no Asset
  Manager work. The official modding tools cannot alter input handling or UI
  navigation — that is compiled into the engine.
- ~~No process memory is read or written. No injection, no hooking, no signature
  scanning.~~ **Amended 2026-10-25:** an optional in-game part may load into
  the game to drive the camera and read game state, under the rules in section
  17. It never writes anything that affects how the game plays out, and the
  controller side must keep working without it.
- No game assets redistributed.
- No changes to balance, drops, stats, or progression.

The controller side synthesizes operating-system input events and nothing else.
That is the most patch-resilient class of tool available, which is why
everything that can live there does, and why it never depends on the in-game
part.

---

## 2. Prior art and reuse policy

Existing solutions fall into two families and neither does what this does.

**Static remappers.** reWASD sells Grim Dawn presets; the community's standard
advice is a Steam Input mode shift that doubles the usable button count. Both
work. Both are configuration rather than software: one binding table, always
active, with no awareness of game state. A remapper's D-pad behaves identically
in a boss fight and in the stash because it has no way to distinguish them.

**Memory patchers.** Grim Internals, now unmaintained and broken by game
updates. DPYes, which replaced it. GrimCam. Perfect Rolls. These analyse and
modify the running game process. More powerful than this project and always will
be — also closed-source, patch-fragile, and dependent on a maintainer who may
stop, which is what happened to Grim Internals.

There is no open-source controller tool for this game. The GitHub `grimdawn`
topic holds save parsers, `.dbr` editors, content mods and text-file filters.

### What makes this different

Context-awareness. A mode stack means one physical input carries different
meanings depending on the open panel. That is a category a preset cannot reach.

Screen-read grid occupancy, so directional input moves item to item rather than
pixel to pixel. Nothing in either family attempts this.

Deliberate refusal to touch the process, trading power for surviving patches.

### Reuse rules

- Do not decompile or extract from Grim Internals, DPYes, GrimCam, Perfect
  Rolls, or reWASD. All are closed-source binaries with no licence grant.
- Binding layouts are design, not code. Studying published reWASD presets and
  Steam Input configs to inform the default binding table is legitimate and
  encouraged.
- GrimAction (github.com/Ophemist/GrimAction) is open source under MIT. Its
  code may be adapted for the in-game part, keeping its copyright notice and
  recording each reuse in `DECISIONS.md`. This does not extend to the
  closed-source tools above.
- Code may be reused from general-purpose open-source input tools — Gopher360,
  AntiMicroX, JoyShockMapper and similar solve stick-to-cursor curves, deadzone
  handling and chorded layers.
- Before copying any line: read the project's `LICENSE` file. If it is GPL,
  do not copy it. Pulling GPL code into an MIT project relicenses the whole
  project, and that is not a decision to make by accident. Record every reuse in
  `DECISIONS.md` with the source and its licence.

---

## 3. Rules for all work

Commit structure, code style and the prohibition on AI references are specified
in the kickoff brief and are binding. Summary, because they matter:

- Past-tense commit messages, 40 to 90 lines each, one idea per commit, never a
  broken build, no attribution trailers of any kind.
- One statement per line. No ternaries. Genuinely short over visually compact.
- No CI configuration. No `.github` directory.
- No reference to AI tooling anywhere in the repository, including commit
  messages and metadata.

Run `dotnet build` and `dotnet test` before every commit.

---

## 4. Environment

- Windows 11, x64. .NET 8 SDK.
- Grim Dawn v1.3.0.0 or later with Fangs of Asterkarn.
- An Xbox-layout gamepad (Cosmic Byte).

### Required game configuration

The tool assumes the game believes it is being played with keyboard and mouse.

- Steam controller configuration for the game: disabled. Steam Input must not be
  translating anything.
- In-game controller support checkbox: off.
- Control scheme: **Keyboard Movement**, not Keyboard Only. Keyboard Only
  removes cursor control, which this tool needs.
- Evade Targeting, if present: player-facing rather than cursor-based. Removes
  one job from the right stick.
- Display: borderless windowed.
- Record resolution and UI scale. Both invalidate calibration when changed.

Mixing native gamepad handling with this tool causes conflicts — this is the
known failure where moving the mouse dismisses the gamepad skill ring. The tool
must be the only translator in the chain.

---

## 5. Repository layout

```
/src
  GDPilot.App/          Host, tray icon, composition root
  GDPilot.Core/         State machine, binding tables, config models, maths
  GDPilot.Input/        SDL reading, calibration, deadzones, curves
  GDPilot.Output/       SendInput wrappers, cursor movement, key synthesis
  GDPilot.Vision/       Screen capture and grid occupancy detection
  GDPilot.Overlay/      WPF click-through heads-up display
  GDPilot.Spike/        Phase 0 self-scoring feasibility probe
  GDPilot.Loopback/     Test target window that records real OS input events
/tests
  GDPilot.Core.Tests/       State machine, maths, config, binding resolution
  GDPilot.Integration.Tests/ Trace-driven end-to-end without hardware
  GDPilot.Vision.Tests/      Occupancy detection against bitmap fixtures
  /fixtures
    traces/             Recorded gamepad input traces as JSON
    screens/            Captured grid screenshots
PLAN.md  PROGRESS.md  DECISIONS.md  TESTING.md  README.md  LICENSE
config.example.json
```

`GDPilot.Core` must not reference `Input`, `Output`, or `Vision`. Dependencies
point inward. This is what makes the state machine and the maths testable
without a controller or a running game, and it is not negotiable.

---

## 6. Verification strategy

Most of this project verifies itself. The architecture below exists for that
reason — build the seams first, before the features that depend on them.

### Two interfaces carry the whole strategy

`IGamepadSource` produces a normalized snapshot of button and axis state per
tick. Two implementations:

- `SdlGamepadSource` reads real hardware.
- `ReplayGamepadSource` plays a JSON trace: a list of entries holding a tick
  offset in milliseconds, a button bitfield, and axis values. Deterministic.

`IInputSink` consumes output intents. Two implementations:

- `SendInputSink` calls into Win32.
- `RecordingSink` appends intents to a list for assertion.

With a replay source feeding the real state machine and the real binding
resolver into a recording sink, an integration test can assert an exact output
sequence for a scripted controller performance. No hardware, no game. This
covers most of Phases 1 through 3 automatically.

### The loopback harness

`GDPilot.Loopback` is a small window that installs low-level keyboard and mouse
hooks and writes every received event to a log file. A test launches it, drives
`SendInputSink` at it, and asserts the log. This proves the P/Invoke layer
actually injects at OS level rather than merely compiling.

It does not prove Grim Dawn accepts the input. Only HT-1 can do that.

### Vision fixtures

`GDPilot.Vision.Tests` generates synthetic grid bitmaps with known occupancy —
draw a grid, fill chosen cells with a non-background colour — and asserts that
detection recovers the pattern. This is fully automatic and can be written
before any real screenshot exists. Once HT-2 supplies real captures, add them to
`/tests/fixtures/screens/` and assert against hand-labelled occupancy.

### What is written first

In every phase, the pure logic goes into `GDPilot.Core` with tests, and the
hardware-facing code is a thin adapter behind an interface. If a piece of logic
cannot be tested without a controller, it is in the wrong project.

### The three human tasks

HT-1, HT-2, HT-3 as specified in the kickoff brief. No others. If you find
yourself wanting a fourth, the answer is a better fixture.

---

## 7. Architecture

**Input reader.** Polls the gamepad through SDL, producing snapshots.

**Mode state machine.** A stack of UI modes. Each mode has its own binding
table. The application knows the mode because it owns the keys that open panels:
pressing the bound inventory button sends the inventory key and pushes
`Inventory`.

**Binding resolver.** Snapshot plus current mode produces output intents.

**Output writer.** Turns intents into injected events.

**Overlay.** Click-through, always on top, showing current mode and legend. Not
optional. A modal input system with no feedback is unusable.

### Modes

```
Suspended    Inert. All synthetic input stopped.
Combat       Default in-world mode.
Inventory    Player bags open.
Stash        Shared or personal stash open.
Character    Character sheet.
Skills       Skill tree.
Devotion     Devotion constellation map.
WorldMap     Full map open.
Vendor       Trade window.
Dialog       Conversation or prompt.
Menu         Escape menu or options.
```

`Suspended` is entered automatically when Grim Dawn is not the foreground
window, and manually by the kill switch. On entry, every held synthetic key must
be released. Leaving keys stuck down at OS level is the worst bug this project
can produce, so the release path must also run on unhandled exception, on
process exit, and from a watchdog thread that fires if the main loop stalls
beyond a threshold.

### Configuration

Single JSON file, hot-reloaded on change. Reload releases held keys first.

```
{
  "activeProfile": "1920x1080@1.0",
  "killSwitch": "Ctrl+Alt+Pause",
  "input": {
    "pollHz": 250,
    "deadzone": { "left": 0.22, "right": 0.18 },
    "calibration": { "leftCenterX": 0.0, "leftCenterY": 0.0 },
    "cursor": { "baseSpeed": 900, "exponent": 2.0, "maxSpeed": 2600 }
  },
  "bindings": {
    "Combat":    { "A": "key:Space", "LT+A": "key:1" },
    "Inventory": { "DPadRight": "grid:right", "A": "mouse:left" }
  },
  "profiles": {
    "1920x1080@1.0": {
      "resolution": [1920, 1080],
      "uiScale": 1.0,
      "grids": {
        "inventory": { "originX": 0, "originY": 0, "pitchX": 0, "pitchY": 0,
                       "columns": 0, "rows": 0 }
      }
    }
  }
}
```

Profile keys combine resolution and UI scale because v1.3 menu windows scale
with UI scaling. On startup, compare live values against the active profile and
refuse grid navigation on mismatch rather than clicking wrong pixels silently.

---

## 8. Phase 0 — Self-scoring feasibility probe

This phase can end the project. Grim Dawn runs on an engine descended from a
2006 codebase, and old engines sometimes ignore synthesized events.

### Tasks

1. `GDPilot.Spike` as a console application.
2. Enumerate top-level windows, printing titles with process names. Do not
   hardcode the window title or executable name — identify from observation.
3. A minimal injection wrapper: key down, key up, mouse move absolute and
   relative, left click, right click. Build the scancode variant at the same
   time as the virtual-key variant, behind a flag. Scancodes are first on the
   fallback ladder and having both ready costs one session instead of two.
4. Build `GDPilot.Loopback` and an automated test that drives the wrapper at it
   and asserts the recorded log. This runs with no game present.
5. Self-scoring game probe. Against the located game window:
   - Capture the client area.
   - Hold the forward key for one second.
   - Capture again. Compute the mean absolute pixel difference over the central
     region.
   - Read the cursor position, issue a relative move, read it again, assert the
     delta.
   - Print a verdict: PASS, FAIL, or INCONCLUSIVE with the measured difference
     score, and write it to `SPIKE_RESULTS.md`.
6. Repeat the probe automatically for each rung of the fallback ladder until one
   passes, and record which one did.

### Fallback ladder

1. Scancodes with `KEYEVENTF_SCANCODE` instead of virtual key codes. Most likely
   fix — many older games read scancodes and ignore virtual-key events.
2. Windowed instead of borderless.
3. Legacy `keybd_event` and `mouse_event`.
4. Elevated process, in case of an integrity-level mismatch.
5. If all four fail, the project needs a kernel-level input driver such as
   Interception. That is a different project with a different risk profile. Stop
   and report rather than starting it.

### HT-1

The human launches the game, stands a character in open ground, alt-tabs, and
runs one executable. It scores itself. They report the printed verdict.

Note the pixel-difference method can be confused by weather, idle animation or
ambient effects — hence the INCONCLUSIVE band. If inconclusive, widen the
sample window and retry automatically before asking again.

Estimated commits: 8 to 12.

---

## 9. Phase 1 — Input plumbing and safety

### Gamepad reading

Use SDL rather than raw XInput. The Cosmic Byte pad is a budget device: it
likely carries a physical X/D mode switch and may enumerate as DirectInput. A
raw XInput implementation would see nothing and appear broken for reasons
unrelated to this code. SDL handles both and maps non-standard pads onto a
standard layout through its controller database.

Evaluate available SDL bindings for .NET, pick one that restores cleanly, record
the choice in `DECISIONS.md`. It goes behind `IGamepadSource`.

### Kill switch — before any autonomous key press

- Low-level keyboard hook for a global hotkey, default `Ctrl+Alt+Pause`.
- On trigger: clear the enabled flag, release every held synthetic key, enter
  `Suspended`.
- The flag is checked immediately before every injection, not once per tick.
- Watchdog thread releasing all keys if the main loop stalls.

### Focus watcher

Poll the foreground window. Suspend when it is not the game, restore on return.

### Stick calibration and shaping

Budget pads drift and do not rest at true centre. Build this now or lose a week
debugging movement code that was never broken.

- Sample both sticks at rest for 500 ms on startup, store the mean as centre.
- Radial deadzone, configurable, generous by default.
- Rescale so output reaches full magnitude at the physical edge.
- Expose a re-calibrate action.

All of this is pure maths in `GDPilot.Core` with unit tests. Only the sampling
touches hardware.

### Mapping

- Left stick to WASD: eight-way above a threshold, holding the corresponding key
  or pair. Movement is digital, so magnitude only matters for a walk threshold.
- Right stick to relative cursor movement: `speed = baseSpeed * magnitude^exponent`,
  clamped to `maxSpeed`, all configurable. Unit-tested.
- Buttons to keys through a flat table.
- Chord layer: while left trigger is held, a second table applies. Roughly
  doubles available inputs and is the direct answer to the button-count problem.

### Verification

Record trace fixtures by hand as JSON, feed them through `ReplayGamepadSource`
into `RecordingSink`, assert exact intent sequences. Covers mapping, chords,
deadzones and curves without hardware.

Estimated commits: 14 to 18.

---

## 10. Phase 2 — Mode state machine and overlay

- Stack with push, pop, clear-to-combat.
- Transitions driven by the application owning panel keys.
- Escape pops one level and forwards the key.
- Panic resync chord — suggested Start plus Back held half a second — clears to
  `Combat` and forwards enough escapes to close open panels. This is the
  recovery path for when tracked state and real UI disagree, which will happen.
- Per-mode binding tables from config.

Transitions are pure logic. Unit-test every path, including malformed
sequences and double-opens. Then extend the trace fixtures to cover cross-mode
scenarios end to end.

### Overlay

WPF window with layered, transparent, no-activate and topmost styles so it never
takes focus and never intercepts clicks. Follows the game window bounds. Shows
current mode prominently, the active legend, and a clear suspended indicator.
Small, cornered, configurable position.

Overlay rendering cannot be asserted. Cover the view-model that feeds it with
unit tests instead, and smoke-test that the window constructs and closes.

Estimated commits: 12 to 16.

---

## 11. Phase 3 — Grid navigation by calibration

### Capture mode — HT-2

Build `--capture` before the wizard. One run dumps screenshots of the bag,
stash and vendor grids plus live resolution and UI scale into
`/tests/fixtures/screens/`. The human opens each panel and presses a key.
Nothing is interpreted by them.

These fixtures then serve both calibration defaults and the Phase 4 tests.

### Calibration wizard

Per profile, run from the overlay.

1. Prompt for open bags.
2. A crosshair moved with the right stick to the centre of the top-left cell.
   Confirm. Record.
3. Repeat for the bottom-right cell.
4. Prompt for column and row counts.
5. Pitch is span divided by count minus one. Store under the profile key.
6. Repeat per grid surface.

Where the captured fixtures allow it, derive an initial guess automatically by
detecting the grid's regular structure, and let the human confirm rather than
place both corners by hand.

Document in `docs/calibration.md` as you build.

### Navigation

- D-pad moves by one cell pitch, clamped to bounds.
- Cursor lands on computed cell centres.
- Face buttons to left click, right click, and modifier-held clicks for
  stack-splitting and quick transfer.
- Bumpers switch stash tabs.
- All coordinate maths in `GDPilot.Core`, unit-tested against known origin,
  pitch and bounds. No game required.

### Startup validation

On resolution or UI scale mismatch, refuse grid navigation and surface a clear
message pointing at recalibration.

Estimated commits: 16 to 20.

---

## 12. Phase 4 — Occupancy detection

Phase 3 steps over empty cells. This makes directional input jump to cells that
hold items, which is the difference between tolerable and good.

### Capture

GDI `BitBlt` over the grid region — lighter than the modern capture API and
sufficient for a static panel. Poll around 8 Hz and only in `Inventory`, `Stash`
or `Vendor`. Never in combat.

### Detection

- Store the empty-slot background colour during calibration.
- Sample a small patch at each cell centre, compare with tolerance.
- Build a boolean occupancy grid.
- Multi-cell items span several cells. Treat contiguous occupied runs as one
  target so navigation does not stop three times inside one breastplate.

### Verification

Generate synthetic grid bitmaps with known occupancy and assert recovery. Vary
cell size, spacing, and background tolerance. Then assert against the real
captures from HT-2 with hand-labelled expected occupancy.

### Navigation upgrade

Directional input moves to the nearest occupied target in that direction,
skipping empties. On low confidence — too many ambiguous cells — fall back
automatically to Phase 3 stepping and show it on the overlay. Degrade, never
freeze.

Estimated commits: 14 to 18.

---

## 13. Phase 5 — Macros and release

**Superseded 2026-10-25 by section 17, where this becomes Phase 8.** Kept for
the detail it still carries.

### Scoped in

- **Fast travel.** Open the world map, template-match the riftgate icon in the
  map region, move to the nearest match, confirm. Template captured during
  calibration. Testable against a captured map screenshot.
- **Vendor quick-sell.** A held-button sweep selling a marked contiguous region.
- **Compound actions.** Town portal and return as single buttons.
- **Tray icon.** Enable, suspend, recalibrate, edit config, quit.
- **First-run setup.** Walk a new user through section 4, then calibration.
- **HT-3, feel tuning.** Ship defaults, make them hot-reloadable, gather
  adjustments in one round.
- **Documentation.** `README.md` and `docs/setup.md` for someone who has never
  seen the project.
- **Release.** Self-contained single-file `win-x64` executable, published
  manually and attached to a GitHub release. No automated pipeline.

### Explicitly cut

Deposit-all macros for components and crafting materials. Grim Dawn v1.3 ships
this natively with dedicated shared stash tabs and instant deposit from anywhere
in the world. Do not reimplement it.

Estimated commits: 18 to 24.

---

## 14. Risk register

| Risk | Impact | Response |
| --- | --- | --- |
| Injection does not reach the game | Fatal | Phase 0, self-scoring, scancode rung first. |
| Stuck synthetic keys | Severe, affects whole OS | Release-all on suspend, on unhandled exception, on exit, and from a watchdog. |
| Pad not detected in DirectInput mode | Blocks all use | SDL rather than raw XInput. Document the X/D switch. |
| Stick drift read as movement | Constant phantom input | Calibration and radial deadzone in Phase 1. |
| UI scale or resolution change | Wrong clicks | Profile keys include both. Refuse to navigate on mismatch. |
| Game patch moves the UI | Recalibration | Pixel-based design means a patch costs a calibration run, not a rewrite. |
| Game patch breaks the in-game part | Camera and exact data lost until fixed | Functions are found by exported name, not address; `--check-game` reports a gap at once; the in-game part refuses to start on any mismatch, and the controller side falls back to screen reading. |
| Antivirus flags the in-game part | Install friction | Public source, reproducible builds, no evasion of any kind. A drop-in loader for the public release, which antivirus treats more kindly than reaching into a running process. |
| Mode desync | Confusing wrong inputs | Panic resync chord, overlay always showing tracked mode. |
| Untestable logic creeping into adapters | Loss of autonomy | Pure logic lives in `Core`. If it needs hardware to test, it is in the wrong project. |
| Scope creep | Project identity | Non-goals in section 1 are fixed. |

---

## 15. Publishing

Safe to publish. Original code and MIT-licensed reuse only, no game assets, no
decompiled code, and no anti-cheat to interact with. The in-game part calls
functions the game exports by name; it does not ship anything taken from the
game.

Requirements: MIT licence, an unaffiliated-with-Crate disclaimer in the README,
minimal screenshots used only where calibration needs explaining, no CI
workflows, and no AI references anywhere per the kickoff brief.

---

## 16. Session protocol

Start of session: read `PROGRESS.md`, then the plan section for the current
phase. Work only within that phase.

End of session: update `PROGRESS.md`, append to `DECISIONS.md` if an open choice
was resolved, ensure the suite is green, and push.

---

## 17. Expanded vision (2026-10-25)

A controller-first overhaul: a third-person action camera, lock-on, radial
skills, menus that snap to items, and controls that adapt to what is on screen.

### Architecture: one install, two parts

- **Controller side** (`GDPilot.exe`, C#, everything built so far). Reads the
  pad, injects input, draws the overlay, owns the kill switch. Never depends on
  the in-game part.
- **In-game part** (a native DLL, C++). Loaded into Grim Dawn when it starts.
  Drives the camera and reports game state over a local named pipe as one-line
  JSON (`GameReport`, protocol 1): open panels, item layouts, later enemy
  positions.
- **Loading.** While building: `GDPilot.exe` loads the in-game part into the
  running game itself, the way GrimAction does. For the public release: a
  drop-in loader in the game folder, so starting the game starts everything.
  The code is the same either way.

### Rules for the in-game part

1. **Find by name.** Every game function is resolved by its exported name.
   Field offsets inside game objects are read from the code of the game's own
   getter functions where possible, so a patch that moves things does not need
   a release. `game-functions.txt` lists every name; `--check-game` checks it.
2. **Fail closed.** Anything missing or unexpected at load means the in-game
   part stays off and says why. The controller side carries on with screen
   reading and the classic camera.
3. **Write only the camera, and the player's own input.** Camera position,
   angle and zoom; and, since the owner's yes on 2026-10-30, the direction the
   left stick asks the character to move, handed to the game the way its own
   gamepad mode hands it. Nothing that changes damage, drops, stats or
   progression, ever.
4. **Never read the pad inside the game.** GrimAction found that polling the
   pad from inside broke the game's own input. The controller side reads it.
5. **The kill switch outranks everything.** It lives in the controller side,
   so it works even if the game hangs.

### Phase 5 — The in-game part and the action camera

- Build toolchain for a native DLL, and the loader.
- Port GrimAction's camera to the installed game build, resolving by name.
- Third-person camera with shoulder swap, collision and zoom, toggled from the
  pad. Right stick looks around in third person; a dot marks the aim point.
- Report open panels (ends mode desync) and exact item layouts (multi-cell
  items become one stop).

### Phase 6 — Combat feel

- **Lock-on.** Nearest or strongest enemy; flick the stick to switch. Needs
  enemy positions from the in-game part.
- **Camera-relative movement.** The left stick's "up" is always away from the
  camera, whatever its turn.
- **Smart interact.** One button for the nearest loot, character or chest.

### Phase 7 — Every skill and every menu

- **Radial skills.** Face buttons stay instant, four more on LT plus face.
  Holding View opens a wheel of every other hotbar slot on the overlay; the
  right stick picks, release casts, the left stick keeps walking. Icons are
  copied from the on-screen hotbar. Pressing a face button while pointing at a
  slot swaps that skill onto it.
- **Rumble.** Low health, heavy hits, rare drops.
- **Snapping in every menu.** Skill tree nodes, devotion stars, dialogue
  choices, the blacksmith's recipe list.
- **Button icons** matching the connected pad.

### Phase 8 — Shortcuts, profiles and release

Section 13's scope, plus profiles per character. Fast travel, quick-sell,
portal and return, tray icon, first-run setup, feel tuning (HT-3), and a
single release download using the drop-in loader.

### The bar for "complete"

The owner set it: playing on a controller must compare with a game made for
one, a soulslike above all, without making Grim Dawn itself any worse.
Lock-on is to match Souls one to one. Grim Dawn's combat stays its own, so
there is no dodge with invulnerability, no stamina, no parry and no strafing
animation; everything else about feel is in scope:

- **Lock-on at Souls parity:** chest aiming, framing off the shoulder,
  recentring on an empty press, gliding between reports. Done.
- **Soft aim** without a lock. Done.
- **Analog movement in any direction**, through the game's own controller
  direction rather than eight keys. Approved by the owner on 2026-10-30; rule
  3 above is amended to allow it.
- **A dodge in the left stick's direction** on one button.
- **Skills that need no cursor:** cast where the character faces, or at the
  lock.
- **Rumble from the fight itself:** hits landed, criticals, kills.
- **A Souls camera:** the stick's up and down tilts rather than zooms, and the
  camera settles behind a running character.

### Phase 9 — Hardening

Before calling the project complete, a phase of its own spent trying to break
it, with a bug pass at the end of every earlier phase too. Game patches, loss
of focus, alt-tab, loading screens, death and respawn, the pad unplugged
mid-fight, full bags, other resolutions and UI scales, the kill switch at the
worst moment, and sessions of hours. Every bug found is fixed with a test that
pins it. The frame cost of the in-game part and the overlay is measured
throughout, since the game must never run worse with GDPilot than without it.

### Shelved

- **Gyro aiming.** Possible through SDL, set aside by the owner on 2026-10-25.
