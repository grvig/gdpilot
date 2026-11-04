# The in-game part's reports

The in-game part runs inside Grim Dawn and talks to GDPilot over the local
pipe `GDPilot.Game`, one JSON line per message, protocol 1. This page lists
what it reports and how it tells the game's windows apart. Every function it
uses is listed in `game-functions.txt`; `GDPilot.exe --check-game` checks
them against an installed game.

## Reports

| Type | Says |
| --- | --- |
| `panels` | The windows open, by mode name |
| `layout` | The items of one bag, the stash tab showing or a trader's goods, in cells |
| `camera` | Whether third person is on |
| `health` | The character's life, in percent |
| `targets` | Enemies near and in view, the character's feet, the lock ring |
| `lockpoint` | Where the locked enemy is this frame |
| `interactables` | Loot, people, fixed things and travel points in reach |
| `abilities` | Whether analog movement is attached |

## Telling the windows apart

| Window | How |
| --- | --- |
| Inventory, skills, world map | A flag of the game's own for each |
| Escape's menu | Two flags raised together |
| Stash | The inventory flag, while the engine records the caravan showing |
| Trader | The inventory flag rising just after the game opens a market |
| Crafter | The inventory flag rising while talking, with neither a market nor a caravan |
| Conversation | The player's talk state, with no window opened since the talk began |
| Devotion map | A byte every large window raises, without the skills', the menu's or the bags' flag |

The talk state lasts until the character next moves, so a conversation left
with B is not followed back into until the report stops. A trader's and a
crafter's marks hold while the inventory flag stays up.

## Spots found on screen

The menus' buttons are not reported: none of the game's menu widgets can be
reached by name. GDPilot finds them on screen at each press instead, as
[calibration.md](calibration.md) describes under "Menus without a grid".
