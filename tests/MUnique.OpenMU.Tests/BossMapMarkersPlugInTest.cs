// <copyright file="BossMapMarkersPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;
using BasicModel = MUnique.OpenMU.Persistence.BasicModel;

/// <summary>
/// Tests for the <see cref="BossMapMarkersPlugIn"/>.
/// </summary>
[TestFixture]
public class BossMapMarkersPlugInTest
{
    private const short HydraNumber = 49;

    private static readonly TimeSpan RespawnDelay = TimeSpan.FromMinutes(70);

    private readonly List<GameServerContext> _gameContexts = new();

    /// <summary>
    /// Disposes the created game contexts.
    /// </summary>
    [TearDown]
    public async Task TearDownAsync()
    {
        foreach (var gameContext in this._gameContexts)
        {
            await gameContext.DisposeAsync().ConfigureAwait(false);
        }

        this._gameContexts.Clear();
    }

    /// <summary>
    /// Tests that a boss name is cut to the 20 bytes (UTF-8) of the name field of a marker, without splitting a character.
    /// </summary>
    /// <param name="name">The name of the boss.</param>
    /// <param name="expected">The expected name in the marker.</param>
    [TestCase("Hydra", "Hydra")]
    [TestCase("Death Spirit (Trainee Soldier)", "Death Spirit (Traine")]
    [TestCase("ÄÄÄÄÄÄÄÄÄÄÄÄ", "ÄÄÄÄÄÄÄÄÄÄ")]
    [TestCase("abcdefghijklmnopqrs😀", "abcdefghijklmnopqrs")]
    public void BossNameIsCutToTheNameField(string name, string expected)
    {
        var result = MUnique.OpenMU.GameServer.RemoteView.World.ShowBossMapMarkersExtendedPlugIn.FitName(name);

        Assert.That(result, Is.EqualTo(expected));
        Assert.That(Encoding.UTF8.GetByteCount(result), Is.LessThanOrEqualTo(MUnique.OpenMU.GameServer.RemoteView.World.ShowBossMapMarkersExtendedPlugIn.NameLength));
    }

    /// <summary>
    /// An alive boss is shown at its position.
    /// </summary>
    [Test]
    public async Task AliveBossIsShownAtItsPositionAsync()
    {
        var (plugIn, monster, _) = await this.CreateSetupAsync(HydraNumber).ConfigureAwait(false);

        var marker = plugIn.GetMarkers(monster.CurrentMap).Single();

        Assert.That(marker.Name, Is.EqualTo("Hydra"));
        Assert.That(marker.IsAlive, Is.True);
        Assert.That(marker.Position, Is.EqualTo(new Point(100, 110)));
    }

    /// <summary>
    /// A killed boss is shown at the place of its death, with the remaining time until its respawn.
    /// </summary>
    [Test]
    public async Task KilledBossIsShownWithItsRespawnTimeAsync()
    {
        var (plugIn, monster, killer) = await this.CreateSetupAsync(HydraNumber).ConfigureAwait(false);

        await monster.ApplyPoisonDamageAsync(killer, 10).ConfigureAwait(false);
        Assert.That(monster.IsAlive, Is.False);
        await plugIn.AttackableGotKilledAsync(monster, killer).ConfigureAwait(false);

        var marker = plugIn.GetMarkers(monster.CurrentMap).Single();
        Assert.That(marker.IsAlive, Is.False);
        Assert.That(marker.Position, Is.EqualTo(new Point(100, 110)));
        Assert.That(marker.RespawnIn, Is.GreaterThan(RespawnDelay - TimeSpan.FromMinutes(1)).And.LessThanOrEqualTo(RespawnDelay));
    }

    /// <summary>
    /// Monsters which aren't configured as bosses aren't shown.
    /// </summary>
    [Test]
    public async Task OtherMonstersAreNotShownAsync()
    {
        var (plugIn, monster, killer) = await this.CreateSetupAsync(1).ConfigureAwait(false);

        Assert.That(plugIn.GetMarkers(monster.CurrentMap), Is.Empty);

        await monster.ApplyPoisonDamageAsync(killer, 10).ConfigureAwait(false);
        await plugIn.AttackableGotKilledAsync(monster, killer).ConfigureAwait(false);
        Assert.That(plugIn.GetMarkers(monster.CurrentMap), Is.Empty);
    }

    /// <summary>
    /// The boss status shows an alive boss as alive, and a killed boss with the remaining time until its respawn.
    /// </summary>
    [Test]
    public async Task BossStatusShowsAliveAndKilledBossAsync()
    {
        var (plugIn, monster, killer) = await this.CreateSetupAsync(HydraNumber).ConfigureAwait(false);
        monster.CurrentMap.Definition.MonsterSpawns.Add(monster.SpawnArea);
        monster.CurrentMap.Definition.Name = "Lorencia";

        var alive = (await plugIn.GetBossStatusesAsync(killer.GameContext).ConfigureAwait(false)).Single();
        Assert.That(alive, Is.EqualTo(new BossStatus("Hydra", "Lorencia", true, TimeSpan.Zero)));

        await monster.ApplyPoisonDamageAsync(killer, 10).ConfigureAwait(false);
        await plugIn.AttackableGotKilledAsync(monster, killer).ConfigureAwait(false);

        var killed = (await plugIn.GetBossStatusesAsync(killer.GameContext).ConfigureAwait(false)).Single();
        Assert.That(killed.IsAlive, Is.False);
        Assert.That(killed.RespawnIn, Is.GreaterThan(RespawnDelay - TimeSpan.FromMinutes(1)).And.LessThanOrEqualTo(RespawnDelay));
    }

    /// <summary>
    /// The bosses of a map which wasn't created yet are alive, because they spawn when the map gets created.
    /// </summary>
    [Test]
    public async Task BossOfNotCreatedMapIsAliveAsync()
    {
        var (plugIn, monster, killer) = await this.CreateSetupAsync(HydraNumber).ConfigureAwait(false);
        var otherMap = new BasicModel.GameMapDefinition { Number = 200, Name = "Atlans" };
        otherMap.MonsterSpawns.Add(new BasicModel.MonsterSpawnArea
        {
            MonsterDefinition = monster.Definition,
            GameMap = otherMap,
            Quantity = 1,
            SpawnTrigger = SpawnTrigger.Automatic,
        });
        killer.GameContext.Configuration.Maps.Add(otherMap);

        var status = (await plugIn.GetBossStatusesAsync(killer.GameContext).ConfigureAwait(false)).Single();

        Assert.That(status, Is.EqualTo(new BossStatus("Hydra", "Atlans", true, TimeSpan.Zero)));
    }

    private async ValueTask<(BossMapMarkersPlugIn PlugIn, Monster Monster, Player Killer)> CreateSetupAsync(short monsterNumber)
    {
        var gameContext = this.CreateGameServerContext();
        var killer = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var map = await gameContext.GetMapAsync(0).ConfigureAwait(false);

        // A unique id, because the attributes of monster definitions are cached by their id.
        var monsterDefinition = new BasicModel.MonsterDefinition
        {
            Id = Guid.NewGuid(),
            Number = monsterNumber,
            Designation = "Hydra",
            RespawnDelay = RespawnDelay,
        };
        var spawnArea = new MonsterSpawnArea
        {
            MonsterDefinition = monsterDefinition,
            GameMap = map!.Definition,
            X1 = 100,
            Y1 = 110,
            X2 = 100,
            Y2 = 110,
            Quantity = 1,
            SpawnTrigger = SpawnTrigger.Automatic,
            MaximumHealthOverride = 1,
        };
        var monster = new Monster(
            spawnArea,
            monsterDefinition,
            map,
            NullDropGenerator.Instance,
            new Mock<INpcIntelligence>().Object,
            gameContext.PlugInManager,
            gameContext.PathFinderPool);
        monster.Initialize();
        await map.AddAsync(monster).ConfigureAwait(false);
        return (new BossMapMarkersPlugIn(), monster, killer);
    }

    private GameServerContext CreateGameServerContext()
    {
        var gameConfiguration = PlayerTestHelper.CreateGameConfiguration();
        var plugInManager = new PlugInManager([], NullLoggerFactory.Instance, null, null);
        var mapInitializer = new MapInitializer(gameConfiguration, new NullLogger<MapInitializer>(), NullDropGenerator.Instance, null);
        var gameServerContext = new GameServerContext(
            new BasicModel.GameServerDefinition
            {
                ServerID = 1,
                GameConfiguration = gameConfiguration,
                ServerConfiguration = new BasicModel.GameServerConfiguration(),
            },
            new Mock<IGuildServer>().Object,
            new Mock<IEventPublisher>().Object,
            new Mock<ILoginServer>().Object,
            new Mock<IFriendServer>().Object,
            new InMemoryPersistenceContextProvider(),
            mapInitializer,
            NullLoggerFactory.Instance,
            plugInManager,
            NullDropGenerator.Instance,
            new ConfigurationChangeMediator());
        mapInitializer.PlugInManager = gameServerContext.PlugInManager;
        mapInitializer.PathFinderPool = gameServerContext.PathFinderPool;

        // Nothing should run in the background, which could affect other tests.
        gameServerContext.StopPeriodicTasks();
        this._gameContexts.Add(gameServerContext);
        return gameServerContext;
    }
}
