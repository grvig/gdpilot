# GDPilot

A context-aware controller layer for Grim Dawn, on Windows.

Grim Dawn's own gamepad support steers a free-floating cursor over menus built
for a mouse. Inventory and stash management are slow, and there are more
actions than buttons. GDPilot runs alongside the game, reads the gamepad
itself, keeps track of which panel is open, and sends the game ordinary
keyboard and mouse input. Because it knows the context, the same button can
mean different things: the D-pad opens panels in combat and steps from item to
item in the stash.

The game is never modified. GDPilot sends operating system input events and
nothing else.

## What GDPilot will do when it is finished

- **Play with a pad.**
  - The left stick walks in eight directions.
  - The right stick moves the cursor, faster the further it is pushed.
  - Every button is configurable.
  - Holding the left trigger switches to a second layer of bindings, which
    roughly doubles what a pad can do.
- **Bindings that follow the open panel.** Combat, Inventory, Stash, Skills,
  Devotion, WorldMap, Vendor, Dialog and Menu each have their own button table.
  Opening a panel from the pad switches to that panel's table automatically.
- **An on-screen overlay.** A small card in a corner of the game shows the
  current mode, what each button does there, and whether input is paused. It
  never takes focus and never catches a click.
- **Grid navigation.** In the inventory, stash and vendor, the D-pad moves the
  cursor exactly one cell at a time instead of steering it by hand. Face
  buttons click, split stacks and quick-transfer, and the bumpers switch stash
  tabs.
- **Jumping between items.** GDPilot reads the inventory grid off the screen,
  so the D-pad jumps straight to the next item in that direction and skips
  empty cells. Items larger than one cell count as one stop. When the screen
  cannot be read reliably, it falls back to cell-by-cell movement and says so.
- **Macros.**
  - One button opens the map and travels to the nearest rift gate.
  - Selling a marked group of items to a vendor in one sweep.
  - Town portal and return as single buttons.
- **Setup and release.**
  - A guided first-run setup and calibration.
  - A tray icon.
  - Settings that apply while running.
  - A single downloadable `.exe`.
- **Safety throughout.**
  - Ctrl+Alt+Pause is a kill switch that stops everything at once.
  - Input pauses whenever the game is not the window in front.
  - A watchdog lets go of any held key if GDPilot freezes, so no key is ever
    left stuck down.

## What it will never do

- Modify game files, archives or database records.
- Read or write the game's memory, inject code or hook the game process.
- Redistribute any game assets.
- Change balance, drops, stats or progression.

Patches cannot break GDPilot the way they break memory-reading tools. At worst
a patch that moves the interface costs a recalibration.

## Where it stands

| Phase | | Status |
| --- | --- | --- |
| 0 | Proof that Grim Dawn accepts synthesized input | **Done.** It does, through keyboard scan codes. |
| 1 | Pad input, movement, cursor, buttons, safety | **Done** |
| 2 | Panel tracking, per-panel bindings, overlay | **Done** |
| 3 | Grid navigation by calibration | Next |
| 4 | Reading item positions off the screen | Not started |
| 5 | Macros, tray, first-run setup, release | Not started |

**Working today:** `GDPilot.exe` reads an Xbox-layout pad through SDL. It is
tested in X mode; D mode is not yet confirmed.

- **Calibration:** it calibrates the sticks at startup.
- **Movement and cursor:** the left stick walks with WASD and the right stick
  drives a curved cursor.
- **Buttons:** the configured buttons and the left-trigger layer work.
- **Panels:**
  - D-pad buttons open the inventory, skills, devotion and map, and Start backs
    out.
  - Each panel switches to its own button table.
  - Start and Back held together resets to Combat if the tracked panel ever
    disagrees with the game.
- **Overlay:** a corner card shows the current mode and buttons.
- **Safety:** the kill switch, the pause when the game is not in front and the
  stuck-key watchdog all work.

**Not yet:**

- Grid navigation, item jumping, macros, the tray icon and a downloadable
  release.
- For now it is a console program built from source.

[PROGRESS.md](PROGRESS.md) has the detail, and [PLAN.md](PLAN.md) the full
design.

## Trying it now

You need the .NET 8 SDK. From the repository root:

```
dotnet build
dotnet run --project src/GDPilot.App -- --dry-run
```

`--dry-run` prints what would be sent instead of sending it, so it is safe with
the game closed. Without it, GDPilot drives the game whenever the game is in
front.

Settings live in `config.json` beside the executable or in the working
directory. Copy [config.example.json](config.example.json) as a starting point.

GDPilot expects the game set up like this:

- **Movement type:** Keyboard (not Keyboard Only), with Forward, Backward, Left
  and Right on W, S, A and D. S opens the skill window by default, so move that
  to another key, such as K.
- **Gamepad:** in-game controller support off, and Steam Input off for Grim
  Dawn.
- **Display:** borderless windowed.

## Requirements

- Windows 11 x64; Windows 10 will probably work but is untested
- Grim Dawn v1.3 or later
- An Xbox-layout gamepad; XInput and DirectInput pads both work through SDL

## Disclaimer

GDPilot is an unofficial tool. It is not affiliated with, endorsed by, or
associated with Crate Entertainment.

## Licence

MIT. See [LICENSE](LICENSE).
