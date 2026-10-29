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
| Left trigger + right stick click | Switch between the game's camera and third person |
| Right stick click | Lock on; the camera turns to face the enemy, see [controls.md](controls.md) |
| Right stick left and right, in third person | Turn the camera; left stick "forward" turns with it |
| Right stick up and down, in third person | Zoom in and out; the camera levels out as it comes closer |
| Left stick sideways, right stick still | The camera swings round toward where you walk |
| F8 | The same switch from the keyboard |

In third person the cursor waits a little ahead of the character, so attacks
and skills go where the camera faces. `aimHeight` under `input` sets how far
down the window that is, `lookSpeed` how fast the stick turns the camera, in
radians per second at full tilt, `zoomSpeed` how fast it zooms, in parts of
the zoom range per second, and `followSpeed` how fast it turns toward a
sideways walk, in radians per second; 0 turns following off. The overlay's
first line says when third person is on.

Switching back restores the game's own camera exactly as it was, down to the
zoom.

## Tuning the view

`%LOCALAPPDATA%\GDPilot\camera.ini` is read each time third person turns on,
so changes apply at the next switch without restarting anything. The first
load writes it with every setting at its default:

```ini
[camera]
distance_min=4
distance_max=30
distance_start=10
pitch_min=12
pitch_max=40
field_of_view=50
collision=1
collision_skin=0.35
collision_extend=18
lock_aim_height=1
lock_turn_speed=8
lock_offset=6
```

| Key | Meaning |
| --- | --- |
| `distance_min`, `distance_max` | How close and how far the mouse wheel can zoom, in world units |
| `distance_start` | Where the zoom starts when third person turns on |
| `pitch_min`, `pitch_max` | The camera's tilt, in degrees, at the nearest and farthest zoom |
| `field_of_view` | In degrees |
| `collision` | 1 keeps the camera in front of anything between it and the character, 0 lets it pass through |
| `collision_extend` | How fast the camera eases back out once clear, in world units per second; 18 by default |
| `collision_skin` | How far short of an obstacle the camera stops; 0.35 by default |
| `lock_aim_height` | How far above an enemy's feet to aim when the game gives no height for it; otherwise lock-on aims at three fifths of the enemy's own height |
| `lock_turn_speed` | How quickly the camera turns to face a locked enemy; higher is snappier |
| `lock_offset` | How far to the right of the character a locked enemy is framed, in degrees; negative frames it to the left |

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
  view, and turns it through the game's own `AdjustYaw`. Beyond the camera it
  only reads: which panels are open, so GDPilot's mode always matches the
  game's, where each item sits in your bags, so a large item is one stop, and
  where the enemies near you stand, for lock-on.
- **On the game's thread.** It attaches to the game's camera update and does
  its work just after the game's own, every frame, so nothing races the game.
- **Never waits.** The pipe to GDPilot is buffered on both sides and read
  with overlapped I/O, so neither side ever stops to wait for the other.
- **Stops cleanly.** If GDPilot exits, is suspended by the kill switch, or the
  game loses focus, the camera stops turning.

The camera's field map, and the order of writing the fields around the game's
own `SetZoom`, come from [GrimAction](https://github.com/Ophemist/GrimAction)
by Ophemist, under its MIT licence.

## Collision

Each frame, the in-game part asks the game's own level query whether anything
lies between the character and the camera. If something does, the camera moves
in front of it at once; when the way is clear again it eases back out to your
chosen zoom. The ray starts a short way out from the character, so it never
meets the character itself. If the game refuses the query thirty times, as it
might during an unusual load, collision switches off for the session and says
so in the log.

## Lock-on

Thirty times a second the in-game part asks the game's own target query, the
one its skills use, for the foes near the player. That query already leaves
out townsfolk, the dead and anything hidden, and asks the game's faction rules
who is a foe. Each foe is then drawn onto the screen through the game's camera
and checked for a clear line through the level query collision uses, so an
enemy behind a wall is never offered. GDPilot picks and switches targets from
those reports; the in-game part only reports, and turns the camera.

The game eases its view toward the camera's yaw rather than jumping, so the
first time third person turns on in a session, the in-game part makes one tiny
turn and watches how the view follows: which way, and how long it takes. The
log records both. From then on a lock counts any turn still on its way, so the
camera glides onto the enemy and stops there instead of swinging past.

Pressing lock-on with nothing to lock asks for a recentre instead: the camera
swings round behind the character, reading which way it faces from the
rotation the game keeps for it, and gives way at once to the right stick.

## What it costs the game

Every half minute the log records how long the in-game part's own work took
per frame, as an average and a worst frame. Measured live on 2026-10-30 with
enemies around, it averaged about 30 microseconds, a fiftieth of a 60 fps
frame, with the worst frame near a third of a millisecond. Switching third
person on reads `camera.ini` and costs a couple of milliseconds, once.

## Not yet

- **Loading without a command.** For release, a loader in the game folder will
  start the in-game part with the game.
