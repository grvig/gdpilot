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
| Inventory, skills, world map, journal, factions, achievements, loot filter | A flag of the game's own for each |
| Escape's menu | Two flags raised together |
| Stash | The inventory flag, while the engine records the caravan showing |
| Trader | The inventory flag rising just after the game opens a market |
| Crafter | The inventory flag with the shared large-window byte, and neither a market nor a caravan |
| Conversation | The player's talk state, with no window opened since the talk began; GDPilot also needs its box on screen |
| Yes or No question | The game's dialog manager holding one |
| Devotion map | A byte every large window raises, without the skills', the menu's or the bags' flag, straight after the skill window |
| Illusionist and other service windows | That same byte while talking to someone |
| Main menu | The game's interface gone for half a second |

The talk state lasts until the character next moves, long after a goodbye, so
GDPilot counts it only while the box shows. A trader's goods are reported sack
by sack, as `Vendor#1` and so on, and the red glow of the tab on screen says
which sack is showing.

## Spots found on screen

The menus' buttons are not reported: none of the game's menu widgets can be
reached by name. GDPilot finds them on screen at each press instead, as
[calibration.md](calibration.md) describes under "Menus without a grid".
