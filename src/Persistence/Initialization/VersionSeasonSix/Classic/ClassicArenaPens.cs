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
/// In the original terrain, each pen is completely surrounded by a wall, so nobody can walk into it.
/// The entrance of a pen is three tiles wide and replaces one segment of its wall model. The game client
/// has its own copy of the terrain and of the wall models (<c>Data\World7\EncTerrain7.att</c> and
/// <c>EncTerrain7.obj</c>), which must have the same entrances; otherwise the client still treats them as walls.
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
    /// Gets the wall tiles which become walkable, grouped by pen.
    /// </summary>
    internal static IReadOnlyList<(byte X, byte Y)> EntranceTiles { get; } =
    [
        // Bottom row of pens, opened to the south.
        (9, 33), (10, 33), (11, 33),
        (28, 33), (29, 33), (30, 33),

        // Right column of pens, opened to the north or to the south.
        (46, 45), (47, 45), (48, 45),
        (46, 63), (47, 63), (48, 63),
        (46, 68), (47, 68), (48, 68),

        // Middle rows of pens, opened to the south.
        (9, 51), (10, 51), (11, 51),
        (28, 51), (29, 51), (30, 51),
        (9, 69), (10, 69), (11, 69),
        (28, 69), (29, 69), (30, 69),

        // Top row of pens, which have a double wall, opened to the east and to the west.
        (17, 90), (17, 91), (17, 92), (18, 90), (18, 91), (18, 92),
        (23, 94), (23, 95), (23, 96), (24, 94), (24, 95), (24, 96),
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
