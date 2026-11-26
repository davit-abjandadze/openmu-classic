---
title: Devil Square and Blood Castle in instances
sidebar_label: Instanced events
sidebar_position: 2
description: Devil Square and Blood Castle several times a day, in rooms per tier of level, entered once per day.
---

# Devil Square and Blood Castle in instances

The Devil Square and the Blood Castle can run in instances: copies of the event
map per tier of level, with the monsters and rewards of the tier. Either all
solo players and parties of a tier share one room per slot (a full room is
followed by another one), or every solo player or party gets its own instance.
The classic data uses shared rooms with three time slots a day.

The mini game definitions decide it: the tiers of an event whose definitions use
the map creation policy **One per party** run in instances. With **Shared**, the
event works as before.

## For players

* `/joinds` enters the Devil Square, `/joinbc` the Blood Castle, from anywhere,
  while the entrance of a slot is open. At other times, the command tells when
  the next entrance opens.
* Each character enters each event **once per day**, in any slot. The attempt
  is used up when entering — leaving, dying or a disconnect don't give it back.
  The day ends at midnight in the time zone of the server. Until its instance
  ends, a player may enter it again, e.g. after dying.
* **Solo:** only the player enters, into the tier of its level.
* **Party:** only the party leader uses the command. Every member must be online,
  alive, on the map of the leader and near it, not in another event, a duel or a
  guild war, and must not have entered the event today. If one member doesn't
  meet the requirements, nobody enters, and the leader learns who and why.
  The party plays the tier of its lowest member. Nobody can join the party
  inside the instance.
* **Level sync:** a member above the maximum level of the tier is weakened to it:
  its damage, defense, attack and defense rate, maximum health and mana are
  multiplied by *maximum level of the tier / own level*. It ends when the player
  leaves the instance, and it's never saved, so nothing stays on the character
  after a disconnect or a server crash. Items, skills and the shown level stay as
  they are; the character window of the client still shows the unweakened values.
  The rewards are those of the entered tier.
* The monsters and the experience and money rewards are scaled by the number of
  players of the instance, so a solo player can finish it while a full party
  doesn't find it too easy. A shared room is scaled by all its players when its
  game starts.

## For server operators

The schedule and the scaling are in the configuration of the plugins
**DevilSquareStartPlugIn** and **BloodCastleStartPlugIn**
([plugins](../admin-panel/plugins.md)):

| Setting | Meaning |
|---|---|
| Timetable | The starts of the slots, in the time zone of the server (see [System](../admin-panel/game-configuration.md#system)). |
| Announcement lead times | When the start is announced before, e.g. 5 and 2 minutes. |
| Upcoming message | The announcement before the start; `{0}` is the number of minutes. |
| Entrance opened / closed message | The announcements when the entrance opens (`{0}` = minutes it stays open) and closes. |
| Entrance duration (instances) | How long the instances can be entered after the start of a slot. All instances of a slot start their game when it ends. |
| Game duration (instances) | The duration of the game of an instance. |
| Task duration | The whole slot; it must be longer than entrance, game and exit together. |
| Shared instance per tier | All solo players and parties of a tier share one room per slot; otherwise each gets its own. The maximum player count of the tier (mini game definition) is the size of a room. |
| Maximum party distance | How near the party members must be to the leader. |
| Maximum instances per slot | Protects the server from too many maps per slot and game server. |
| Level sync | Whether members above the maximum level of the tier are weakened. |
| Party size scalings | Per number of players: the factors of the number of monsters (every spawn keeps at least one, so bosses stay), of the monster health (bosses, gates and statues included), and of the experience and money rewards. Bigger groups use the entry of the highest player count. Item rewards are not scaled. |

The tiers and their level windows are the mini game definitions of the event
([mini games](../admin-panel/game-configuration.md#mini-games)): every active
definition with **One per party** is a tier, with its minimum and maximum
character level. Changing the times, windows or factors takes effect at the
next slot.

The classic data starts the Devil Square at 13:00, 19:00 and 22:00 and the
Blood Castle at 13:30, 19:30 and 22:30 (Asia/Tbilisi), with an entrance of
5 minutes, a game of 15 minutes and shared rooms of up to 50 players. The Blood Castle has 7 tiers (10–60,
61–120, 121–180, 181–240, 241–300, 301–360, 361–400); the Devil Square has 6,
because the seventh Devil Square came after Season 1, so its sixth tier covers
the levels 301–400.

:::note
The daily entries are stored as stat attributes of the characters
(*Devil Square Entry Day*, *Blood Castle Entry Day*). A character class without
them can't enter, because the limit couldn't be applied.
:::
