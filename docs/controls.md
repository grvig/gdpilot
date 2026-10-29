# Playing with a pad

Everything below is what [config.example.json](../config.example.json) binds.
Every button can be rebound in `config.json`; the overlay in the corner of the
game always shows what each button does in the mode you are in.

## In the world

| Button | Does |
| --- | --- |
| Left stick | Walk, in eight directions |
| Right stick | Move the cursor, faster the further it is pushed |
| Right bumper | Left click: attack or interact where the cursor is |
| Left bumper | Right click |
| A | Space |
| X, Y, B | Hotbar slots 2, 3 and 4 |
| Left trigger + A, X, Y, B | Hotbar slots 1, 5, 6 and 7 |
| Back, held | The skill wheel |
| Right stick click | Lock on to the enemy in view; again to let go |
| Left trigger + right stick click | Switch between the game's camera and third person |
| D-pad up, left, right, down | Inventory, devotion, skills, map |
| Left trigger + D-pad up, down | Tell GDPilot a trader's or the stash's window is open |
| Start | Back out of the top panel, or open the game's menu |
| Start and Back, held | Resynchronise: close everything back to combat |

## The skill wheel

Hold Back and a wheel of the hotbar's ten slots appears over the game, 1 at the
top and the rest clockwise, each showing its skill's icon. Point the right
stick at a slot and let go of Back to cast it. Letting the stick return before
Back changes nothing, so a flick and release is the whole gesture. The left
stick keeps walking and the face buttons keep working while the wheel is open.

The wheel's slots are `radialSlots` under `input`, 1 to 0 by default. The
icons come from the hotbar calibrated as the `Hotbar` grid; without it the
wheel shows the keys instead.

## Lock-on

With the in-game part loaded, click the right stick and GDPilot locks on to the
enemy you are most plainly facing: the one nearest the middle of the view,
with nearer enemies preferred. A ring marks it, with a bar of its life, and the
cursor stays on it, so attacks and skills land on it wherever it runs.

| Control | Does |
| --- | --- |
| Right stick click | Lock on; again to let go |
| Right stick, flicked | Move the lock to the next enemy that way on the screen |

- **One flick, one switch.** The stick has to come back toward the middle
  before the next flick counts, so holding it over never skips past anyone.
- **In third person the camera turns to face the lock**, smoothly, framing
  the enemy a little to the right of your character so it is never hidden
  behind you. `lock_offset` in `camera.ini` sets how far, in degrees.
- **The ring sits on the enemy's chest**, whatever its size, and glides with it
  between the game's reports.
- **With nothing to lock onto, the click swings the camera round behind your
  character**, the way action games do.
- **When the enemy falls, the lock moves on** to the enemy nearest where it
  stood, so a fight flows from one to the next.
- **Walls block it.** An enemy behind a wall or a building is never offered.
- **It lets go** when the enemy gets a third further away than `lockRange`
  under `input` (25 world units by default), stays out of view for three
  seconds, or when a panel opens.

The pad ticks lightly as a lock is taken or switched, and a little more firmly
as one is lost.

### Soft aim

Without a lock, in third person, the cursor drifts from the aim point onto an
enemy close to where the camera points, and back when it moves away, so a
swing aimed roughly at an enemy lands on it. It only takes enemies near your
character and near the middle of the view, and keeps the one it has until it
moves well away. `softAim` under `input` turns it off.

## Third person

With the in-game part loaded, holding the left trigger and clicking the right
stick drops the camera behind your character. Then:

| Stick | Does |
| --- | --- |
| Right stick left and right | Turn the camera; walking turns with it |
| Right stick up and down | Zoom in and out; the camera levels out as it comes closer |
| Left stick sideways, right stick still | The camera swings round toward where you walk |

Attacks go where the camera faces: the cursor waits a little ahead of the
character. [camera.md](camera.md) has the details and the settings.

## In the bags, at the stash and with a trader

| Button | Does |
| --- | --- |
| D-pad | Jump to the next item in that direction, skipping empty cells |
| Left bumper | Next bag (with a trader, also the trader's goods) |
| A | Left click: pick up or put down |
| X | Right click: use or equip |
| Left trigger + A | Ctrl+click: split a stack |
| B or Start | Close the panel |

At the stash the bumpers switch tabs instead, and Back moves between the stash
and the bags. An item larger than one cell is a single stop when the in-game
part is loaded, since the game then says exactly where each item sits.

## Rumble

With the in-game part loaded the pad gives a short jolt when a heavy blow lands,
a heartbeat when life is low that quickens as it runs out, and a light tick as
lock-on takes, switches or loses an enemy. `rumble` under
`input` turns it off. `GDPilot.exe --rumble-test` buzzes each motor in turn, to
check the pad has rumble at all.

## Safety

| Key | Does |
| --- | --- |
| Ctrl+Alt+Pause | Stop all input at once; again to resume |

Input also pauses whenever the game is not the window in front, and any key
GDPilot was holding is let go if it ever stalls.
