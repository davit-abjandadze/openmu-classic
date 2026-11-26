// <copyright file="ClassicPvpSpots.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.PvP;

/// <summary>
/// Creates the default PvP spots of the classic server: two per map, from Lorencia to Icarus.
/// </summary>
/// <remarks>
/// The spots are 25x25 squares around hunting areas which are far enough from the safezone.
/// They are the spots which the server runs with; they can be adjusted in the configuration of the <see cref="PvpSpotsPlugIn"/>.
/// </remarks>
public static class ClassicPvpSpots
{
    /// <summary>
    /// The numbers of the maps which get spots: Lorencia, Dungeon, Devias, Noria, Lost Tower, Atlans, Tarkan and Icarus.
    /// </summary>
    public static readonly IReadOnlyList<short> MapNumbers = [0, 1, 2, 3, 4, 7, 8, 10];

    private static readonly (string Name, short MapNumber, byte X1, byte Y1, byte X2, byte Y2)[] Spots =
    [
        ("Lorencia 1", 0, 39, 115, 63, 139),
        ("Lorencia 2", 0, 191, 155, 215, 179),
        ("Dungeon 1", 1, 157, 110, 181, 134),
        ("Dungeon 2", 1, 231, 0, 255, 20),
        ("Devias 1", 2, 95, 111, 119, 135),
        ("Devias 2", 2, 195, 128, 219, 152),
        ("Noria 1", 3, 164, 9, 188, 33),
        ("Noria 2", 3, 81, 44, 105, 68),
        ("Lost Tower 1", 4, 91, 188, 115, 212),
        ("Lost Tower 2", 4, 13, 199, 37, 223),
        ("Atlans 1", 7, 11, 218, 35, 242),
        ("Atlans 2", 7, 107, 12, 131, 36),
        ("Tarkan 1", 8, 123, 58, 147, 82),
        ("Tarkan 2", 8, 172, 104, 196, 128),
        ("Icarus 1", 10, 74, 29, 98, 53),
        ("Icarus 2", 10, 41, 154, 65, 178),
    ];

    /// <summary>
    /// Creates the spots of the maps which exist in the game configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The spots.</returns>
    public static IEnumerable<PvpSpotsConfiguration.PvpSpot> Create(GameConfiguration gameConfiguration)
    {
        foreach (var spot in Spots.Where(s => gameConfiguration.Maps.Any(m => m.Number == s.MapNumber && m.Discriminator == 0)))
        {
            yield return new PvpSpotsConfiguration.PvpSpot
            {
                Name = spot.Name,
                MapNumber = spot.MapNumber,
                X1 = spot.X1,
                Y1 = spot.Y1,
                X2 = spot.X2,
                Y2 = spot.Y2,
            };
        }
    }
}
