# C2 FC - FarmSpotMapMarkersExtended (by server)

## Is sent when

A player entered a map.

## Causes the following actions on the client side

The farm spots of the map, small places where a few monsters of the same kind stand together, are shown as dots on the full-screen map (Tab) of the open source client. Replaces all previously sent farm spots of the same map. The original client ignores this packet.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC2  | [Packet type](PacketTypes.md) |
| 1 | 2 |    Short   |      | Packet header - length of the packet |
| 3 | 1 |    Byte   | 0xFC  | Packet header - packet type identifier |
| 4 | 1 | Byte |  | MapNumber |
| 5 | 1 | Byte |  | Count |
| 6 | FarmSpotMarker.Length * Count | Array of FarmSpotMarker |  | Spots |

### FarmSpotMarker Structure

A farm spot of the map.

Length: 2 Bytes

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 | Byte |  | PositionX; The x coordinate of the centre of the spot. |
| 1 | 1 | Byte |  | PositionY; The y coordinate of the centre of the spot. |