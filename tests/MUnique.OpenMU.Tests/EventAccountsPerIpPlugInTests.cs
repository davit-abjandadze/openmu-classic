// <copyright file="EventAccountsPerIpPlugInTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.TeamVsTeam;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests for the <see cref="EventAccountsPerIpPlugIn"/>, which lets only one account of an IP address take part in the same event.
/// </summary>
[TestFixture]
public class EventAccountsPerIpPlugInTests
{
    private const string IpAddress = "10.0.0.1";

    private readonly List<MiniGameContext> _gamesToDispose = [];

    /// <summary>
    /// Disposes the created games.
    /// </summary>
    [TearDown]
    public async Task TearDownAsync()
    {
        foreach (var game in this._gamesToDispose)
        {
            await game.DisposeAsync().ConfigureAwait(false);
        }

        this._gamesToDispose.Clear();
    }

    /// <summary>
    /// Tests that another account of the same IP address can't enter the event, even after the first one left it.
    /// </summary>
    [Test]
    public async Task OtherAccountOfSameIpIsRefusedAsync()
    {
        var plugIn = new EventAccountsPerIpPlugIn();
        var gameContext = GameContextTestHelper.CreateGameContext();
        var game = this.CreateGame(gameContext, MiniGameType.DevilSquare);

        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "second", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.False);
    }

    /// <summary>
    /// Tests that the account which entered the event may enter it again, e.g. with a new connection or after it died.
    /// </summary>
    [Test]
    public async Task SameAccountIsAdmittedAgainAsync()
    {
        var plugIn = new EventAccountsPerIpPlugIn();
        var gameContext = GameContextTestHelper.CreateGameContext();
        var game = this.CreateGame(gameContext, MiniGameType.DevilSquare);

        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "FIRST", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.True);
    }

    /// <summary>
    /// Tests that accounts of other IP addresses, players without an IP address (e.g. bots), and other events aren't affected.
    /// </summary>
    [Test]
    public async Task OtherIpOtherEventAndBotsAreAdmittedAsync()
    {
        var plugIn = new EventAccountsPerIpPlugIn();
        var gameContext = GameContextTestHelper.CreateGameContext();
        var devilSquare = this.CreateGame(gameContext, MiniGameType.DevilSquare);
        var bloodCastle = this.CreateGame(gameContext, MiniGameType.BloodCastle);

        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false), devilSquare).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "other", "10.0.0.2").ConfigureAwait(false), devilSquare).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "bot", null).ConfigureAwait(false), devilSquare).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "second", IpAddress).ConfigureAwait(false), bloodCastle).ConfigureAwait(false), Is.True);
    }

    /// <summary>
    /// Tests that a run of the same event on another game server counts, too.
    /// </summary>
    [Test]
    public async Task RunOnOtherGameServerCountsAsync()
    {
        var plugIn = new EventAccountsPerIpPlugIn();
        var gameContext = GameContextTestHelper.CreateGameContext();
        var otherGameContext = GameContextTestHelper.CreateGameContext();
        var game = this.CreateGame(gameContext, MiniGameType.DevilSquare);
        var gameOnOtherServer = this.CreateGame(otherGameContext, MiniGameType.DevilSquare);

        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(otherGameContext, "second", IpAddress).ConfigureAwait(false), gameOnOtherServer).ConfigureAwait(false), Is.False);
    }

    /// <summary>
    /// Tests that the entrants of a run which is over don't count for the next run of the event.
    /// </summary>
    [Test]
    public async Task RunWhichIsOverDoesNotCountAsync()
    {
        var plugIn = new EventAccountsPerIpPlugIn();
        var gameContext = GameContextTestHelper.CreateGameContext();
        var previousGame = this.CreateGame(gameContext, MiniGameType.DevilSquare);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false), previousGame).ConfigureAwait(false), Is.True);
        await previousGame.DisposeAsync().ConfigureAwait(false);

        var game = this.CreateGame(gameContext, MiniGameType.DevilSquare);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "second", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.True);
    }

    /// <summary>
    /// Tests that the configured maximum of accounts per IP address applies.
    /// </summary>
    [Test]
    public async Task ConfiguredMaximumAppliesAsync()
    {
        var plugIn = new EventAccountsPerIpPlugIn { Configuration = new EventAccountsPerIpConfiguration { MaximumAccountsPerIp = 2 } };
        var gameContext = GameContextTestHelper.CreateGameContext();
        var game = this.CreateGame(gameContext, MiniGameType.DevilSquare);

        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "second", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.True);
        Assert.That(await AdmitAsync(plugIn, await CreatePlayerAsync(gameContext, "third", IpAddress).ConfigureAwait(false), game).ConfigureAwait(false), Is.False);
    }

    /// <summary>
    /// Tests that the mini game asks the active plugin when a player enters it, and refuses the other account of the same IP address.
    /// </summary>
    [Test]
    public async Task MiniGameRefusesOtherAccountOfSameIpAsync()
    {
        var gameContext = GameContextTestHelper.CreateGameContext([new PlugInConfiguration { TypeId = typeof(EventAccountsPerIpPlugIn).GUID, IsActive = true }]);
        var game = this.CreateGame(gameContext, MiniGameType.DevilSquare);

        Assert.That(await game.TryEnterAsync(await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false)).ConfigureAwait(false), Is.EqualTo(EnterResult.Success));
        var second = await CreatePlayerAsync(gameContext, "second", IpAddress).ConfigureAwait(false);
        Assert.That(await game.TryEnterAsync(second).ConfigureAwait(false), Is.EqualTo(EnterResult.Failed));
        Assert.That(second.CurrentMiniGame, Is.Null);
    }

    /// <summary>
    /// Tests that the Team vs Team refuses the registration of another account of the same IP address.
    /// </summary>
    [Test]
    public async Task TeamVsTeamRefusesOtherAccountOfSameIpAsync()
    {
        var gameContext = GameContextTestHelper.CreateGameContext([new PlugInConfiguration { TypeId = typeof(EventAccountsPerIpPlugIn).GUID, IsActive = true }]);
        Assert.That(EventAccountsPerIpPlugIn.GetActive(gameContext), Is.Not.Null);
        var teamVsTeam = new TeamVsTeamPlugIn { Configuration = TeamVsTeamConfiguration.Default };
        var state = teamVsTeam.GetState(gameContext);
        state.State = PeriodicTaskState.Prepared;
        var first = await CreatePlayerAsync(gameContext, "first", IpAddress).ConfigureAwait(false);
        var second = await CreatePlayerAsync(gameContext, "second", IpAddress).ConfigureAwait(false);

        await teamVsTeam.TryRegisterAsync(first).ConfigureAwait(false);
        await teamVsTeam.TryRegisterAsync(second).ConfigureAwait(false);

        Assert.That(state.Registered, Is.EquivalentTo(new[] { first }));
    }

    private static ValueTask<bool> AdmitAsync(EventAccountsPerIpPlugIn plugIn, Player player, MiniGameContext game)
        => plugIn.TryAdmitAsync(player, game.Definition.Type, game, () => game.State is MiniGameState.Open or MiniGameState.Closed or MiniGameState.Playing);

    private static async ValueTask<Player> CreatePlayerAsync(IGameContext gameContext, string loginName, string? ipAddress)
    {
        var player = new IpTestPlayer(gameContext, ipAddress);
        await PlayerTestHelper.EnterWorldAsync(player, PlayerTestHelper.CreateCharacter(gameContext)).ConfigureAwait(false);
        player.Account = new Account { LoginName = loginName };
        return player;
    }

    private static MiniGameDefinition CreateDefinition(IGameContext gameContext, MiniGameType type)
    {
        var definitionMock = new Mock<MiniGameDefinition>();
        definitionMock.SetupGet(d => d.Rewards).Returns(new List<MiniGameReward>());
        definitionMock.SetupGet(d => d.SpawnWaves).Returns(new List<MiniGameSpawnWave>());
        definitionMock.SetupGet(d => d.ChangeEvents).Returns(new List<MiniGameChangeEvent>());
        definitionMock.SetupGet(d => d.Entrance).Returns(new ExitGate { Map = gameContext.Configuration.Maps.First(m => m.Number == 0) });
        var definition = definitionMock.Object;
        definition.Type = type;
        definition.MapCreationPolicy = MiniGameMapCreationPolicy.Shared;
        definition.MaximumPlayerCount = 10;
        return definition;
    }

    private MiniGameContext CreateGame(IGameContext gameContext, MiniGameType type)
    {
        var mapInitializer = new Mock<IMapInitializer>();
        mapInitializer.Setup(m => m.CreateGameMap(It.IsAny<GameMapDefinition>()))
            .Returns<GameMapDefinition>(map => new GameMap(map, TimeSpan.FromMinutes(1), 16));
        var game = new MiniGameContext(new MiniGameMapKey(0, 0, string.Empty), CreateDefinition(gameContext, type), gameContext, mapInitializer.Object);
        this._gamesToDispose.Add(game);
        return game;
    }

    private sealed class IpTestPlayer : Player, IHasIpAddress
    {
        public IpTestPlayer(IGameContext gameContext, string? ipAddress)
            : base(gameContext)
        {
            this.IpAddress = ipAddress;
        }

        public string? IpAddress { get; }

        protected override ICustomPlugInContainer<IViewPlugIn> CreateViewPlugInContainer()
        {
            return new MockViewPlugInContainer();
        }
    }
}
