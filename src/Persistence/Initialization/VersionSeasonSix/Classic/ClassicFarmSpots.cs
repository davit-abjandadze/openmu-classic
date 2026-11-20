// <copyright file="ClassicFarmSpots.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Creates the farm spots of the classic server: many tiny areas in which a few monsters of the same kind stand
/// on one place, so that players can stand next to them and kill them without walking around.
/// </summary>
/// <remarks>
/// Each map from Lorencia to Icarus gets up to <see cref="SpotsPerMap"/> spots of <see cref="MonstersPerSpot"/> monsters.
/// The spots are shared round-robin by the normal monsters of the map, sorted by level. A spot is placed
/// in or next to the area where its monster lives anyway, on a square of walkable tiles outside of the safezone,
/// as far as possible from the other spots and not too near to the safezone.
/// Additionally, there are the <see cref="FixedSpots"/>, which were chosen by hand.
/// The spots are normal monster spawn areas, so they can be adjusted in the map editor of the admin panel.
/// </remarks>
public static class ClassicFarmSpots
{
    /// <summary>
    /// The number of spots per map.
    /// </summary>
    public const int SpotsPerMap = 35;

    /// <summary>
    /// The number of monsters of a spot.
    /// </summary>
    public const short MonstersPerSpot = 8;

    /// <summary>
    /// The distance between the centre and the border of the square of a spot, so it's 3 x 3 tiles big.
    /// </summary>
    public const int Radius = 1;

    /// <summary>
    /// The minimum number of walkable tiles outside of the safezone in the square of a spot.
    /// </summary>
    public const int MinimumFreeTiles = 8;

    /// <summary>
    /// The minimum distance between two spots.
    /// </summary>
    public const int MinimumDistanceBetweenSpots = 6;

    /// <summary>
    /// The minimum distance between a spot and the centre of the safezone.
    /// </summary>
    public const int MinimumDistanceToSafezone = 15;

    /// <summary>
    /// The first number of the monster spawn areas of the spots, so that their identifiers don't collide with the original spawns.
    /// </summary>
    public const short FirstSpawnNumber = 0x0F00;

    /// <summary>
    /// The numbers of the maps which get farm spots: Lorencia, Dungeon, Devias, Noria, Lost Tower, Atlans, Tarkan and Icarus.
    /// </summary>
    public static readonly IReadOnlyList<short> MapNumbers = [0, 1, 2, 3, 4, 7, 8, 10];

    /// <summary>
    /// The first number of the monster spawn areas of the <see cref="FixedSpots"/>.
    /// </summary>
    public const short FirstFixedSpawnNumber = 0x0F80;

    /// <summary>
    /// The farm spots which were chosen by hand, additionally to the automatically placed ones.
    /// Their order must not change, because the identifier of a spawn area depends on the index.
    /// </summary>
    public static readonly IReadOnlyList<FixedSpot> FixedSpots =
    [
        // Devias: Elite Yeti
        new(2, 20, 216, 214),
        new(2, 20, 239, 214),
        new(2, 20, 239, 235),
        new(2, 20, 223, 171),
        new(2, 20, 190, 154),
        new(2, 20, 201, 207),
        new(2, 20, 198, 237),

        // Devias: Yeti
        new(2, 19, 37, 13),
        new(2, 19, 36, 38),
        new(2, 19, 21, 41),

        // Devias: Worm
        new(2, 24, 242, 83),
    ];

    private const int SearchMargin = 4;

    /// <summary>
    /// Adds the farm spots to the maps or updates them. It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var mapNumber in MapNumbers)
        {
            if (gameConfiguration.Maps.FirstOrDefault(m => m.Number == mapNumber && m.Discriminator == 0) is not { TerrainData: not null } map)
            {
                continue;
            }

            var fixedSpots = new List<Spot>();
            for (var index = 0; index < FixedSpots.Count; index++)
            {
                var fixedSpot = FixedSpots[index];
                if (fixedSpot.MapNumber == mapNumber
                    && gameConfiguration.Monsters.FirstOrDefault(m => m.Number == fixedSpot.MonsterNumber) is { } monster)
                {
                    var spot = new Spot(monster, fixedSpot.X, fixedSpot.Y);
                    ApplySpawn(context, map, (short)(FirstFixedSpawnNumber + index), spot);
                    fixedSpots.Add(spot);
                }
            }

            var spots = FindSpots(map, fixedSpots);
            for (var index = 0; index < spots.Count; index++)
            {
                ApplySpawn(context, map, (short)(FirstSpawnNumber + index), spots[index]);
            }

            // Spots of an earlier version which aren't found anymore.
            foreach (var obsolete in map.MonsterSpawns.Where(s => GetSpotIndex(s) >= spots.Count).ToList())
            {
                map.MonsterSpawns.Remove(obsolete);
            }
        }
    }

    /// <summary>
    /// Gets the identifier of the monster spawn area of a spot.
    /// </summary>
    /// <param name="mapNumber">The map number.</param>
    /// <param name="spawnNumber">The number of the spawn, beginning with <see cref="FirstSpawnNumber"/>.</param>
    /// <returns>The identifier.</returns>
    internal static Guid GetSpawnId(short mapNumber, short spawnNumber) => GuidHelper.CreateGuid<MonsterSpawnArea>(mapNumber, spawnNumber);

    /// <summary>
    /// Determines whether the spawn area is one of the farm spots.
    /// </summary>
    /// <param name="spawn">The spawn area.</param>
    /// <returns><c>true</c>, if the spawn area is a farm spot.</returns>
    internal static bool IsFarmSpot(MonsterSpawnArea spawn) => GetSpotIndex(spawn) >= 0 || IsFixedSpot(spawn);

    /// <summary>
    /// Determines whether the spawn area is one of the <see cref="FixedSpots"/>.
    /// </summary>
    /// <param name="spawn">The spawn area.</param>
    /// <returns><c>true</c>, if the spawn area is a fixed farm spot.</returns>
    internal static bool IsFixedSpot(MonsterSpawnArea spawn)
    {
        var mapNumber = spawn.GameMap?.Number ?? -1;
        var id = spawn.GetId();
        return Enumerable.Range(0, FixedSpots.Count).Any(i => GetSpawnId(mapNumber, (short)(FirstFixedSpawnNumber + i)) == id);
    }

    private static int GetSpotIndex(MonsterSpawnArea spawn)
    {
        var mapNumber = spawn.GameMap?.Number ?? -1;
        var id = spawn.GetId();
        for (var index = 0; index < SpotsPerMap; index++)
        {
            if (GetSpawnId(mapNumber, (short)(FirstSpawnNumber + index)) == id)
            {
                return index;
            }
        }

        return -1;
    }

    private static List<Spot> FindSpots(GameMapDefinition map, IReadOnlyCollection<Spot> fixedSpots)
    {
        var terrain = new GameMapTerrain(map);
        var safezone = map.ExitGates.FirstOrDefault(g => g.IsSpawnGate);
        var safeX = safezone is null ? -1000 : (safezone.X1 + safezone.X2) / 2;
        var safeY = safezone is null ? -1000 : (safezone.Y1 + safezone.Y2) / 2;
        var bossNumbers = ClassicBosses.Bosses.Select(b => b.MonsterNumber).ToHashSet();

        var monsters = map.MonsterSpawns
            .Where(s => s.SpawnTrigger == SpawnTrigger.Automatic && !IsFarmSpot(s))
            .Where(s => s.MonsterDefinition is { ObjectKind: NpcObjectKind.Monster, IsActive: true } monster
                        && !bossNumbers.Contains(monster.Number)
                        && monster.RespawnDelay <= TimeSpan.FromMinutes(1))
            .GroupBy(s => s.MonsterDefinition!)
            .OrderBy(g => GetLevel(g.Key))
            .ThenBy(g => g.Key.Number)
            .Select(g => (Monster: g.Key, Candidates: GetCandidates(terrain, g, safeX, safeY)))
            .Where(m => m.Candidates.Count > 0)
            .ToList();

        var spots = new List<Spot>();
        var placedOne = true;
        while (spots.Count < SpotsPerMap && placedOne)
        {
            placedOne = false;
            foreach (var (monster, candidates) in monsters)
            {
                if (spots.Count >= SpotsPerMap)
                {
                    break;
                }

                if (FindFarthestCandidate(candidates, spots, fixedSpots) is { } center)
                {
                    spots.Add(new Spot(monster, center.X, center.Y));
                    placedOne = true;
                }
            }
        }

        return spots;
    }

    /// <summary>
    /// Gets the possible spot centres in and next to the areas where the monster lives.
    /// </summary>
    private static List<(int X, int Y)> GetCandidates(GameMapTerrain terrain, IEnumerable<MonsterSpawnArea> areas, int safeX, int safeY)
    {
        var result = new HashSet<(int X, int Y)>();
        foreach (var area in areas)
        {
            var minX = Math.Max(Radius, Math.Min(area.X1, area.X2) - SearchMargin);
            var maxX = Math.Min(255 - Radius, Math.Max(area.X1, area.X2) + SearchMargin);
            var minY = Math.Max(Radius, Math.Min(area.Y1, area.Y2) - SearchMargin);
            var maxY = Math.Min(255 - Radius, Math.Max(area.Y1, area.Y2) + SearchMargin);
            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    if (Distance(x, y, safeX, safeY) >= MinimumDistanceToSafezone
                        && CountFreeTiles(terrain, x, y) >= MinimumFreeTiles)
                    {
                        result.Add((x, y));
                    }
                }
            }
        }

        // Sorted, so that the result doesn't depend on the order of the hash set.
        return result.OrderBy(c => c.X).ThenBy(c => c.Y).ToList();
    }

    /// <summary>
    /// Gets the candidate which is farthest from all other spots, so that the spots are spread over the whole area of the monster.
    /// </summary>
    private static (int X, int Y)? FindFarthestCandidate(List<(int X, int Y)> candidates, List<Spot> spots, IReadOnlyCollection<Spot> fixedSpots)
    {
        (int X, int Y)? best = null;
        var bestDistance = (double)MinimumDistanceBetweenSpots;
        var otherSpots = spots.Concat(fixedSpots).ToList();
        foreach (var candidate in candidates)
        {
            var nearest = otherSpots.Count == 0
                ? double.MaxValue
                : otherSpots.Min(s => Distance(candidate.X, candidate.Y, s.X, s.Y));
            if (nearest > bestDistance || (best is null && nearest >= bestDistance))
            {
                bestDistance = nearest;
                best = candidate;
            }
        }

        return best;
    }

    private static int CountFreeTiles(GameMapTerrain terrain, int centerX, int centerY)
    {
        var count = 0;
        for (var x = centerX - Radius; x <= centerX + Radius; x++)
        {
            for (var y = centerY - Radius; y <= centerY + Radius; y++)
            {
                if (terrain.WalkMap[x, y] && !terrain.SafezoneMap[x, y])
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void ApplySpawn(IContext context, GameMapDefinition map, short number, Spot spot)
    {
        var id = GetSpawnId(map.Number, number);
        var spawn = map.MonsterSpawns.FirstOrDefault(s => s.GetId() == id);
        if (spawn is null)
        {
            spawn = context.CreateNew<MonsterSpawnArea>();
            spawn.SetGuid(id);
            map.MonsterSpawns.Add(spawn);
        }

        spawn.GameMap = map;
        spawn.MonsterDefinition = spot.Monster;
        spawn.Quantity = MonstersPerSpot;
        spawn.Direction = Direction.Undefined;
        spawn.SpawnTrigger = SpawnTrigger.Automatic;
        spawn.WaveNumber = 0;
        spawn.X1 = (byte)(spot.X - Radius);
        spawn.X2 = (byte)(spot.X + Radius);
        spawn.Y1 = (byte)(spot.Y - Radius);
        spawn.Y2 = (byte)(spot.Y + Radius);
    }

    private static float GetLevel(MonsterDefinition monster)
        => monster.Attributes.FirstOrDefault(a => a.AttributeDefinition?.Id == Stats.Level.Id)?.Value ?? 0;

    private static double Distance(double x1, double y1, double x2, double y2) => Math.Sqrt(((x1 - x2) * (x1 - x2)) + ((y1 - y2) * (y1 - y2)));

    /// <summary>
    /// A farm spot which was chosen by hand.
    /// </summary>
    /// <param name="MapNumber">The map number.</param>
    /// <param name="MonsterNumber">The number of the monster.</param>
    /// <param name="X">The x coordinate of the centre.</param>
    /// <param name="Y">The y coordinate of the centre.</param>
    public sealed record FixedSpot(short MapNumber, short MonsterNumber, byte X, byte Y);

    private sealed record Spot(MonsterDefinition Monster, int X, int Y);
}
