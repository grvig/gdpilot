# Grid calibration

Grid navigation moves the cursor from cell to cell in the bags, the stash and
the vendor window. To do that, GDPilot needs to know exactly where those cells
are on screen. That knowledge is the calibration, and it is stored in the
configuration under `profiles`.

## Why calibration is tied to a display

Grim Dawn's menu windows scale with its UI scale setting. Changing either the
resolution or the UI scale moves every cell. So each profile is named after
both, for example `1920x1080@0.5`, and records them:

```json
"activeProfile": "1920x1080@0.5",
"profiles": {
  "1920x1080@0.5": {
    "resolution": [1920, 1080],
    "uiScale": 0.5,
    "grids": { ... }
  }
}
```

At startup GDPilot reads the game's window size, and the UI scale from the
game's own `options.txt`. It compares both with the active profile. If either
differs, grid navigation is refused, and the console says why and which
profile to calibrate. A mismatch means clicking the wrong cells, so it never
navigates on one.

## How a grid is described

```json
"Inventory": { "originX": 652, "originY": 645, "pitchX": 32, "pitchY": 32, "columns": 12, "rows": 8 }
```

| Field | Meaning |
| --- | --- |
| `originX`, `originY` | The centre of the top-left cell, in physical screen pixels. |
| `pitchX`, `pitchY` | The distance between the centres of neighbouring cells. |
| `columns`, `rows` | How many cells the grid has. |

Zeros mean "not calibrated", and such a grid is ignored.

Grids are named after the mode they belong to. A mode with several grids uses
names like `Inventory` and `Inventory.Bag2`; the `grid:next` binding switches
between them. The D-pad's `grid:up`, `grid:down`, `grid:left` and `grid:right`
move one cell and stop at the edges. The first press after entering a mode
lands on the top-left cell.

## The shipped calibration

`config.example.json` carries a measured profile for 1920×1080 at UI scale 0.5:

| Grid | Top-left centre | Pitch | Size |
| --- | --- | --- | --- |
| `Inventory`, the main bag | (652, 645) | 32 | 12 × 8 |
| `Inventory.Bag2`, the second bag | (1044, 645) | 32 | 8 × 8 |
| `Vendor`, the trader's goods | (483, 323) | 32 | 10 × 18 |
| `Vendor.Bag1`, the main bag | (824, 645) | 32 | 12 × 8 |
| `Vendor.Bag2`, the second bag | (1217, 645) | 32 | 8 × 8 |

The bags appear twice because the game draws them in two places. With a
trader's window open, the inventory panel moves right by 172 pixels and stays
at the same height. A grid per surface, rather than per mode, would have
clicked the wrong cells while trading.

The numbers were measured from the captures in `tests/fixtures/screens`.
Empty cells are dark and edged with thin bright lines. Averaging brightness
along rows and columns through a grid gives sharp peaks every 32 pixels, and
cell centres sit halfway between them.

Two checks confirmed the bag grids:
- **Live.** Moving the cursor to the computed centre of each bag's top-left
  cell showed that item's tooltip.
- **Automated.** `CalibrationFixtureTests` checks the shipped numbers against
  the captures on every integration test run. It fails if an origin moves by
  half a cell. `GridDetectionFixtureTests` checks that the detector finds all
  five grids on its own.

## Measuring a capture

`GDPilot.exe --detect <image> <left> <top> <right> <bottom>` looks for a grid
inside a rectangle of a saved capture and prints it in the configuration's
format:

```
GDPilot.exe --detect tests/fixtures/screens/inventory.png 628 620 1024 895
{ "originX": 651, "originY": 645, "pitchX": 32, "pitchY": 32, "columns": 12, "rows": 8 }
```

The rectangle should hug the grid, a few pixels outside its outer lines. A
much wider one can take in the panel's border, whose lines fall on the same
spacing, and report an extra row. The detector looks for runs of evenly spaced
brightness peaks, so item art inside cells does not throw it off as long as
most lines are visible. When it finds nothing, it says so rather than guessing.
A pixel off in the origin, as here, makes no difference to a click.

The wizard below does not need a rectangle. It searches outward from the two
cells it is shown, widening step by step, and only accepts a grid whose
corner cells sit under those two points.

## Calibrating a new display

1. Set the game's resolution and UI scale, and open the panel to calibrate.
2. Run `GDPilot.exe --calibrate <GridName>`, for example
   `--calibrate Stash` or `--calibrate Vendor.Bag1`. The name must start with
   the mode the grid belongs to.
3. Move the cursor with the right stick to the centre of the top-left cell
   and press A, then to the centre of the bottom-right cell and press A.
4. If the grid lines between those points can be read, the wizard shows what
   it found. If not, set the column and row counts with the D-pad: left and
   right for columns, up and down for rows. Pitch follows from the two points.
5. Press A to save, or B to start again. B at any other step cancels.

The grid is written into `config.json` in the working directory, under the
profile for the live display, which becomes the active profile. A first save
starts from `config.example.json`, so its bindings come along. The file is
read and written back whole, so comments in it are not kept.

A tab strip is calibrated the same way, with the first and last tabs as the
two points. Points within a few pixels of level make a single row. Name it
after its mode with `.Tabs`, as in `Stash.Tabs`. The bumpers' `tab:next` and
`tab:previous` click through it and wrap at the ends. `grid:next` skips it.

To calibrate by hand instead, run `GDPilot.exe --capture <name>` to save the
client area and its profile, then measure with `--detect` or an image editor.
Pitch is the span divided by the count minus one: `(lastX - firstX) / (columns - 1)`.

## Splitting stacks

A `mouse:` binding can carry one modifier, held across the whole click:
`mouse:ctrl+left`, `mouse:shift+right`, `mouse:alt+left`. The example binds
Ctrl+Left click to LT+A in the panel modes. The modifier goes down before the
button and comes up after it, so the game never sees a bare click.

## Known gaps

- **The stash** has no capture, grids or tab strip yet. Its bindings are in
  the example, waiting for `--calibrate Stash` and `--calibrate Stash.Tabs`.
- **Live checks** of the vendor grids, the wizard, tab switching and modifier
  clicks are still to do. The bag grids are the only ones confirmed in the game.
