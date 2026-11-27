// <copyright file="ClassicArenaPens.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Maps;

/// <summary>
/// Opens an entrance into each of the eleven walled pens in the west of the Arena, around which the
/// Arena farm spots are placed (see <see cref="ClassicFarmSpots"/>).
/// </summary>
/// <remarks>
/// Each pen has a doorway between two pillars in its wall model, but the original terrain blocks it,
/// so nobody can walk into the pen. This opens the terrain of the doorways. The game client has its own copy
/// of the terrain (<c>Data\World7\EncTerrain7.att</c>), which must have the same doorways opened;
/// otherwise the client still treats them as walls.
/// </remarks>
public static class ClassicArenaPens
{
    /// <summary>
    /// The size of the header of the terrain data, before the attributes of the tiles.
    /// </summary>
    private const int TerrainHeaderSize = 3;

    /// <summary>
    /// The number of tiles in one row of the terrain.
    /// </summary>
    private const int TerrainSize = 256;

    /// <summary>
    /// Gets the wall tiles of the doorways which become walkable, grouped by pen.
    /// </summary>
    internal static IReadOnlyList<(byte X, byte Y)> EntranceTiles { get; } =
    [
        // Left column of pens, doorways in the east wall.
        (16, 37), (16, 38), (16, 39), (16, 40),
        (16, 55), (16, 56), (16, 57), (16, 58),
        (16, 73), (16, 74), (16, 75), (16, 76),

        // Middle column of pens, doorways in the east wall.
        (35, 37), (35, 38), (35, 39), (35, 40),
        (35, 55), (35, 56), (35, 57), (35, 58),
        (35, 73), (35, 74), (35, 75), (35, 76),

        // Right column of pens, doorways in the east wall.
        (53, 37), (53, 38), (53, 39), (53, 40),
        (53, 55), (53, 56), (53, 57), (53, 58),
        (53, 81), (53, 82), (53, 83),

        // Top row of pens: the doorway in the south-east corner of the left one and in the south wall of the right one.
        (16, 86), (17, 86), (16, 87), (17, 87),
        (28, 86), (29, 86), (30, 86), (31, 86),
    ];

    /// <summary>
    /// Makes the <see cref="EntranceTiles"/> walkable. Other flags and all other tiles are kept.
    /// It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(GameConfiguration gameConfiguration)
    {
        const int minimumLength = TerrainHeaderSize + (TerrainSize * TerrainSize);
        const byte blocked = (byte)TerrainAttributeType.Blocked;
        foreach (var map in gameConfiguration.Maps.Where(map => map is { Number: Arena.Number, Discriminator: 0 }))
        {
            if (map.TerrainData is not { Length: >= minimumLength } terrainData
                || EntranceTiles.All(tile => (terrainData[GetOffset(tile)] & blocked) == 0))
            {
                continue;
            }

            // A new array, so that the persistence layer recognizes the change.
            var changedTerrainData = terrainData.ToArray();
            foreach (var tile in EntranceTiles)
            {
                changedTerrainData[GetOffset(tile)] &= unchecked((byte)~blocked);
            }

            map.TerrainData = changedTerrainData;
        }
    }

    /// <summary>
    /// Gets the offset of the attribute of a tile in the terrain data.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>The offset.</returns>
    internal static int GetOffset((byte X, byte Y) tile) => TerrainHeaderSize + (tile.Y * TerrainSize) + tile.X;
}
