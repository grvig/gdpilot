# Playing with a pad

Everything below is what [config.example.json](../config.example.json) binds.
Every button can be rebound in `config.json`; the overlay in the corner of the
game always shows what each button does in the mode you are in.

## In the world

| Button | Does |
| --- | --- |
| Left stick | Walk, in any direction with the in-game part loaded, else in eight |
| Right stick | Aim at the ground round your character the way it points (in panels, a free cursor) |
| Right bumper | Left click: attack or interact where the cursor is |
| Left bumper | Right click |
| A | Interact with what is in reach: talk, use, pick up; else Space |
| X, Y | Hotbar slots 2 and 3 |
| B | Dodge: hotbar slot 4, aimed the way the left stick points |
| Left trigger + A, X, Y, B | Hotbar slots 1, 5, 6 and 7 |
| Back, held | The skill wheel |
| Right stick click | Lock on to the enemy in view; again to let go |
| Left trigger + right stick click | Switch between the game's camera and third person |
| D-pad up, left, right, down | Inventory, devotion, skills, map |
| Left trigger + D-pad up, down | Tell GDPilot a trader's or the stash's window is open (only needed without the in-game part) |
| Left trigger + D-pad left, right | The journal, the factions |
| Left trigger + Back, Start | Achievements, the loot filter |
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

### Dodge

B fires whatever skill sits in hotbar slot 4 at the ground a few steps out the
way the left stick points, so a movement skill such as Blitz, Shadow Strike or
a dash becomes a dodge in any direction, without aiming the cursor. With the
stick at rest it goes straight ahead. Put your movement skill in slot 4, or
bind `dodge:<key>` under `bindings` to another slot. The in-game part reports
where the ground round your character falls on the screen, so the aim is right
in either camera; without it the dodge aims out from the middle of the screen.

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
| Right stick up and down | Tilt the view; with the left trigger held, zoom instead |
| Left stick sideways, right stick still | The camera swings round toward where you walk |

Attacks go where the camera faces: the cursor waits a little ahead of the
character. [camera.md](camera.md) has the details and the settings.

## In the bags, at the stash and with a trader

| Button | Does |
| --- | --- |
| D-pad | Jump to the next item in that direction, skipping empty cells |
| Left bumper or Back | Next bag, then the gear |
| A | Left click: pick up or put down |
| X | Right click: use or equip |
| Left trigger + A | Ctrl+click: split a stack |
| Left trigger + D-pad up | In the bags: a trader's window is open (only needed without the in-game part) |
| B or Start | Close the panel |

At the stash and with a trader the bumpers switch tabs instead, and Back moves
between the goods, the bags and the gear. An item larger than one cell is a single stop when the in-game
part is loaded, since the game then says exactly where each item sits.

With the in-game part loaded, GDPilot sees the stash and a trader's window
open by themselves and reads the stash tab showing and the trader's goods
from the game, so their items are exact too.

## In the skill tree

| Button | Does |
| --- | --- |
| D-pad | Jump to the nearest skill in that direction |
| Bumpers | The other mastery's tree |
| A | Left click: put a point in |
| X | Right click |
| Back | Back to the first skill |
| Y, B or Start | Close the window |

The skills are found on screen at each press, so the D-pad follows whichever
tree is showing; a skill hidden under a tooltip stays within reach. The
mastery bar's plus button and Undo Points are stops too.

## On the devotion map

The devotion map is the skill window's middle tab: D-pad left opens it from
combat, and the bumpers move from a mastery's tree to it and on to the other
mastery. Zoomed fully out, the whole map fits on screen. On the map the D-pad steps
from star to star in the direction pressed, A takes a star, X right-clicks,
and the zoom buttons at the top are stops too. Back returns to the first star.

## At a blacksmith

A crafter's window opens beside the bags, and GDPilot switches to its buttons
by itself. The D-pad moves over the category tabs, the recipe list and the
Combine button; the bumpers change category, and Back moves on to each bag and
the gear. A picks or clicks, X right-clicks, and LT+A splits a stack.

## In a conversation and the game's menu

When the character talks to someone, GDPilot switches to the conversation's
buttons by itself. The D-pad moves between the replies, A picks one, and B
leaves the conversation. Walking away ends it too. In the game's menu, from
Start, the D-pad moves between its entries and A picks one; in Options it moves
between the tabs and buttons, the right stick's cursor reaching the settings
themselves. A Yes or No question takes the buttons wherever it appears.

## The journal and other windows

In the journal, the factions, the achievements and the loot filter, the D-pad
steps between what can be clicked, A clicks and B or Start closes.

## Aiming in the game's own camera

Out in the world, the right stick points the cursor at the ground round your
character, further out the further it leans, so skills go where the stick
points; let go, the aim follows your walk. An enemy near the aim draws it.
In panels the stick moves a free cursor as before. `twinStick` under `input`
turns this off.

## Interacting

A picks up loot, talks to the townsfolk, travels through rift gates and
portals, and uses shrines, chests, doors and levers, whichever is nearest
within six units, without aiming at it; loot wins when two are about as near.
A prompt low on the screen, such as "A  Pick up" or "A  Travel", says what A
would do. With nothing in reach, A is Space as before. An attack held on the
bumper carries on through the click.

## Walking

With the in-game part loaded, the left stick walks your character at the exact
angle it points, relative to the camera, not just the eight directions of
WASD. GDPilot still holds the nearest of the eight keys, since the game only
walks while one is down, and the in-game part turns that walk to the stick's
angle. If the in-game part is not loaded, or a game update has moved what it
needs, walking carries on in eight directions. The overlay says "Left stick:
walks in any direction" while the full range is on. `analogMovement` under
`input` turns it off.

## Rumble

With the in-game part loaded the pad gives a short jolt when a heavy blow lands
on you, a heartbeat when life is low that quickens as it runs out, and a light
tick as lock-on takes, switches or loses an enemy.

The fight is felt too: a light tick as an enemy close by, or the one locked,
is wounded, a firmer knock when one blow takes a big piece of its life, and a
thump as one dies. These come from the same reports lock-on reads, so a
companion's blows are felt as well. `--rumble-test` plays all of them. `rumble` under
`input` turns it off. `GDPilot.exe --rumble-test` buzzes each motor in turn, to
check the pad has rumble at all.

## The overlay

A small card in the style of the game's own panels names the mode, says what
is going on (third person, a lock, walking in any direction) and lists what
every button does there. Five seconds after the mode changes it folds down to
its title and those status lines, and opens out again at the next change.

It never covers the game's own interface: it knows where the minimap, the
quest tracker, the orbs and hotbar, and each panel sit, and takes the first
corner clear of all of them, starting with `corner` under `overlay` (top left
by default). It stays above the game, but under any other window brought in
front of it, so it never floats over a browser or a chat.

## Safety

| Key | Does |
| --- | --- |
| Ctrl+Alt+Pause | Stop all input at once; again to resume |

Input also pauses whenever the game is not the window in front, and any key
GDPilot was holding is let go if it ever stalls.
