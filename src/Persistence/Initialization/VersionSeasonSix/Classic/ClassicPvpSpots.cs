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
/// The spots are placed around the hunting areas with the most monsters, which are far enough from the safezone.
/// They can be adjusted in the configuration of the <see cref="PvpSpotsPlugIn"/>.
/// </remarks>
public static class ClassicPvpSpots
{
    /// <summary>
    /// The numbers of the maps which get spots: Lorencia, Dungeon, Devias, Noria, Lost Tower, Atlans, Tarkan and Icarus.
    /// </summary>
    public static readonly IReadOnlyList<short> MapNumbers = [0, 1, 2, 3, 4, 7, 8, 10];

    private const int SpotsPerMap = 2;
    private const int HalfSize = 12;
    private const int MinimumDistanceToSafezone = 35;
    private const int MinimumDistanceBetweenSpots = 40;

    /// <summary>
    /// Creates the spots.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The spots.</returns>
    public static IEnumerable<PvpSpotsConfiguration.PvpSpot> Create(GameConfiguration gameConfiguration)
    {
        foreach (var mapNumber in MapNumbers)
        {
            if (gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber && m.Discriminator == 0) is not { } map)
            {
                continue;
            }

            var safezone = map.ExitGates.FirstOrDefault(g => g.IsSpawnGate);
            var safeX = safezone is null ? -1000 : (safezone.X1 + safezone.X2) / 2;
            var safeY = safezone is null ? -1000 : (safezone.Y1 + safezone.Y2) / 2;
            var mapName = map.Name.ValueInNeutralLanguage ?? $"Map {mapNumber}";

            var candidates = map.MonsterSpawns
                .Where(s => s.MonsterDefinition is { ObjectKind: NpcObjectKind.Monster, IsActive: true })
                .Select(s => (X: (s.X1 + s.X2) / 2, Y: (s.Y1 + s.Y2) / 2, s.Quantity))
                .Where(c => Distance(c.X, c.Y, safeX, safeY) >= MinimumDistanceToSafezone)
                .GroupBy(c => (c.X / 16, c.Y / 16))
                .Select(g => (X: (int)g.Average(c => c.X), Y: (int)g.Average(c => c.Y), Quantity: g.Sum(c => c.Quantity)))
                .OrderByDescending(c => c.Quantity)
                .ToList();

            var chosen = new List<(int X, int Y)>();
            foreach (var candidate in candidates)
            {
                if (chosen.Count >= SpotsPerMap)
                {
                    break;
                }

                if (chosen.All(c => Distance(c.X, c.Y, candidate.X, candidate.Y) >= MinimumDistanceBetweenSpots))
                {
                    chosen.Add((candidate.X, candidate.Y));
                }
            }

            var number = 1;
            foreach (var (x, y) in chosen)
            {
                yield return new PvpSpotsConfiguration.PvpSpot
                {
                    Name = $"{mapName} {number++}",
                    MapNumber = mapNumber,
                    X1 = (byte)Math.Clamp(x - HalfSize, 0, 255),
                    Y1 = (byte)Math.Clamp(y - HalfSize, 0, 255),
                    X2 = (byte)Math.Clamp(x + HalfSize, 0, 255),
                    Y2 = (byte)Math.Clamp(y + HalfSize, 0, 255),
                };
            }
        }
    }

    private static double Distance(int x1, int y1, int x2, int y2) => Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));
}
