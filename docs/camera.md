# The third-person camera

GDPilot can drop Grim Dawn's camera from high above the character to just
behind it, and turn it with the right stick. That is done by its in-game part,
a small DLL that loads into the game. Everything else in GDPilot works without
it.

## Using it

1. Build the in-game part once. It needs the Visual Studio 2022 Build Tools
   with the C++ workload:

   ```
   src\GDPilot.InGame\build.cmd
   ```

   The build fetches Microsoft Detours at one pinned commit and refuses it if
   its fingerprint differs. Then `dotnet build` copies the DLL beside
   `GDPilot.exe`.

2. Start Grim Dawn, then load the in-game part into it:

   ```
   GDPilot.exe --load-in-game
   ```

   It runs `--check-game` first and refuses on any mismatch. Once loaded, it
   stays until the game exits; a new build needs a game restart.

3. Run GDPilot as usual. The in-game part connects to it within two seconds.

| Control | What it does |
| --- | --- |
| Right stick click | Switch between the game's camera and third person |
| Right stick, in third person | Turn the camera; left stick "forward" turns with it |
| F8 | The same switch from the keyboard |

In third person the cursor waits a little ahead of the character, so attacks
and skills go where the camera faces. `aimHeight` under `input` sets how far
down the window that is, and `lookSpeed` how fast the stick turns the camera,
in radians per second at full tilt.

Switching back restores the game's own camera exactly as it was, down to the
zoom.

## Tuning the view

`%LOCALAPPDATA%\GDPilot\camera.ini` is read each time third person turns on,
so changes apply at the next switch without restarting anything:

```ini
[camera]
distance_min=4
distance_max=30
distance_start=10
pitch_min=12
pitch_max=40
field_of_view=50
```

| Key | Meaning |
| --- | --- |
| `distance_min`, `distance_max` | How close and how far the mouse wheel can zoom, in world units |
| `distance_start` | Where the zoom starts when third person turns on |
| `pitch_min`, `pitch_max` | The camera's tilt, in degrees, at the nearest and farthest zoom |
| `field_of_view` | In degrees |

The game tilts the camera between the two pitches as the zoom moves between
the two distances, so a close camera also looks along the ground rather than
down at it. For reference, the game's own camera runs from distance 20 to 48
and pitch 38 to 52.

## How it stays safe

- **Found by name.** The in-game part finds every game function it uses by
  the name the game's own DLLs export, then checks each one still starts with
  the bytes it was mapped from. Those bytes contain the camera field offsets
  it writes, so a match is what makes the writes safe. Any difference and it
  stays off, saying why in `%LOCALAPPDATA%\GDPilot\in-game.log`.
- **Camera only.** It writes the camera's distance, tilt, zoom and field of
  view, and turns it through the game's own `AdjustYaw`. Nothing else.
- **On the game's thread.** It attaches to the game's camera update and does
  its work just after the game's own, every frame, so nothing races the game.
- **Never waits.** The pipe to GDPilot is buffered on both sides and read
  with overlapped I/O, so neither side ever stops to wait for the other.
- **Stops cleanly.** If GDPilot exits, is suspended by the kill switch, or the
  game loses focus, the camera stops turning.

The camera's field map, and the order of writing the fields around the game's
own `SetZoom`, come from [GrimAction](https://github.com/Ophemist/GrimAction)
by Ophemist, under its MIT licence.

## Not yet

- **Camera collision.** The camera can sit inside a wall or behind a post.
- **Tilt from the stick.** The stick's up and down does nothing yet.
- **Following the character.** The camera keeps its heading until turned.
- **Loading without a command.** For release, a loader in the game folder will
  start the in-game part with the game.
