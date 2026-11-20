// <copyright file="FarmSpotMapMarkersPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.Pathfinding;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the <see cref="FarmSpotMapMarkersPlugIn"/>.
/// </summary>
[TestFixture]
public class FarmSpotMapMarkersPlugInTest
{
    /// <summary>
    /// A spawn area of 8 monsters on a 3 x 3 square is shown at its centre.
    /// </summary>
    [Test]
    public void FarmSpotIsShownAtItsCentre()
    {
        var map = CreateMap(CreateSpawn(8, 99, 109, 101, 111));

        var spots = new FarmSpotMapMarkersPlugIn().GetSpots(map);

        Assert.That(spots, Is.EquivalentTo(new[] { new Point(100, 110) }));
    }

    /// <summary>
    /// Normal spawn areas aren't shown: other quantities, bigger areas, NPCs and spawns of events.
    /// </summary>
    [Test]
    public void OtherSpawnAreasAreNotShown()
    {
        var npcSpawn = CreateSpawn(8, 10, 10, 12, 12);
        npcSpawn.MonsterDefinition!.ObjectKind = NpcObjectKind.PassiveNpc;
        var eventSpawn = CreateSpawn(8, 20, 20, 22, 22);
        eventSpawn.SpawnTrigger = SpawnTrigger.OnceAtEventStart;

        var map = CreateMap(
            CreateSpawn(5, 30, 30, 32, 32),
            CreateSpawn(8, 40, 40, 60, 60),
            npcSpawn,
            eventSpawn);

        var spots = new FarmSpotMapMarkersPlugIn().GetSpots(map);

        Assert.That(spots, Is.Empty);
    }

    private static GameMapDefinition CreateMap(params MonsterSpawnArea[] spawns)
    {
        var map = new BasicModel.GameMapDefinition { Number = 0 };
        foreach (var spawn in spawns)
        {
            map.MonsterSpawns.Add(spawn);
        }

        return map;
    }

    private static MonsterSpawnArea CreateSpawn(short quantity, byte x1, byte y1, byte x2, byte y2)
    {
        return new BasicModel.MonsterSpawnArea
        {
            MonsterDefinition = new BasicModel.MonsterDefinition { ObjectKind = NpcObjectKind.Monster },
            Quantity = quantity,
            SpawnTrigger = SpawnTrigger.Automatic,
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
        };
    }
}
