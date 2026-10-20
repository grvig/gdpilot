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

It was measured from `tests/fixtures/screens/inventory.png`, captured with the
bags open. Empty cells are dark and edged with thin bright lines. Averaging
brightness along rows and columns through empty cells gives sharp peaks every
32 pixels: vertical lines at 1028, 1060 … 1284, and horizontal lines at 629,
661 … 885. Cell centres sit halfway between lines.

Two checks confirmed it:
- **Live.** Moving the cursor to the computed centre of each bag's top-left
  cell showed that item's tooltip.
- **Automated.** `CalibrationFixtureTests` checks the shipped numbers against
  the capture on every test run. It fails if the origin moves by half a cell.

## Calibrating a new display

The calibration wizard is not built yet. Until then:

1. Set the game's resolution and UI scale, open the panel, and run
   `GDPilot.exe --capture inventory`. That saves `captures/inventory.png` and a
   `.json` naming the profile, such as `1920x1080@0.5`.
2. In the image, find the centres of the top-left and bottom-right cells.
   Pitch is the span divided by the count minus one:
   `(lastX - firstX) / (columns - 1)`.
3. Add a profile under that name with the grid, and set `activeProfile` to it.

## Known gaps

- **The bags move when a vendor is open.** With the trade window up, the
  inventory panel sits further left and higher than when it is open alone, so
  the bag grids for Vendor mode need their own entries. They are not measured
  yet. `tests/fixtures/screens/vendor.png` holds the capture to measure from.
- **The stash** has no capture or grid yet.
- **The wizard,** and detecting a grid automatically from a capture, are still
  to come.
