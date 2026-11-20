# C2 FB - BossMapMarkersExtended (by server)

## Is sent when

A player entered a map, or a boss of the player's current map got killed or respawned.

## Causes the following actions on the client side

The bosses of the map are shown on the full-screen map (Tab) of the open source client, with the remaining time until a killed boss respawns. Replaces all previously sent bosses of the same map. The original client ignores this packet.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFB  | Packet header - packet type identifier |
| 4 | 1 | Byte |  | MapNumber |
| 5 | 1 | Byte |  | Count |
| 6 | BossMarker.Length * Count | Array of BossMarker |  | Bosses |

### BossMarker Structure

A boss of the map.

Length: 28 Bytes

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 | Byte |  | PositionX |
| 1 | 1 | Byte |  | PositionY |
| 2 | 1 | Boolean |  | IsAlive |
| 4 | 4 | IntegerLittleEndian |  | RespawnSeconds; The seconds until the boss respawns, when it's not alive. The position is the place of its death then. |
| 8 | 20 | String |  | Name |