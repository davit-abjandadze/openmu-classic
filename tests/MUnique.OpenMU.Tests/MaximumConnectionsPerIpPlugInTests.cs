// <copyright file="MaximumConnectionsPerIpPlugInTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;


using System.ComponentModel.Design;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Unit tests for the <see cref="MaximumConnectionsPerIpPlugIn"/>.
/// </summary>
[TestFixture]
public class MaximumConnectionsPerIpPlugInTests
{
    private GameContext _gameContext = null!;
    private MaximumConnectionsPerIpPlugIn _plugin = null!;

    /// <summary>
    /// Sets up the game context and the plug-in with a limit of three connections per IP address.
    /// </summary>
    [SetUp]
    public async Task SetUp()
    {
        var dummyPlayer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        this._gameContext = (GameContext)dummyPlayer.GameContext;
        this._plugin = new MaximumConnectionsPerIpPlugIn
        {
            Configuration = new MaximumConnectionsPerIpPlugInConfiguration
            {
                MaximumConnectionsPerIp = 3
            }
        };
    }

    /// <summary>
    /// Tests that a joining player below the connection limit is not cancelled.
    /// </summary>
    [Test]
    public async ValueTask UnderLimitSucceedsAsync()
    {
        // Arrange
        var player1 = new TestPlayer(this._gameContext);
        player1.SetIpAddress("127.0.0.1");
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player1).ConfigureAwait(false);

        var player2 = new TestPlayer(this._gameContext);
        player2.SetIpAddress("127.0.0.1");
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player2).ConfigureAwait(false);

        var joiningPlayer = new TestPlayer(this._gameContext);
        joiningPlayer.SetIpAddress("127.0.0.1");
        await joiningPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);

        var eventArgs = new StateMachine.StateChangeEventArgs { NextState = PlayerState.Authenticated };

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that a joining player at the connection limit is cancelled.
    /// </summary>
    [Test]
    public async ValueTask AtLimitCancelsTransitionAsync()
    {
        // Arrange
        var player1 = new TestPlayer(this._gameContext);
        player1.SetIpAddress("127.0.0.1");
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player1).ConfigureAwait(false);

        var player2 = new TestPlayer(this._gameContext);
        player2.SetIpAddress("127.0.0.1");
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player2).ConfigureAwait(false);

        var player3 = new TestPlayer(this._gameContext);
        player3.SetIpAddress("127.0.0.1");
        await player3.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player3.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player3).ConfigureAwait(false);

        var joiningPlayer = new TestPlayer(this._gameContext);
        joiningPlayer.SetIpAddress("127.0.0.1");
        await joiningPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);

        var eventArgs = new StateMachine.StateChangeEventArgs { NextState = PlayerState.Authenticated };

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.True);
        Assert.That(joiningPlayer.LoginResultOverride, Is.EqualTo(GameLogic.Views.Login.LoginResult.ServerIsFull));
    }

    /// <summary>
    /// Tests that a player of the same IP address which is disconnecting doesn't count anymore,
    /// like for the maximum players of the game server.
    /// </summary>
    [Test]
    public async ValueTask DisconnectingPlayerDoesNotCountAsync()
    {
        // Arrange
        for (var i = 0; i < 3; i++)
        {
            var player = new TestPlayer(this._gameContext);
            player.SetIpAddress("127.0.0.1");
            await player.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
            await player.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
            await this._gameContext.AddPlayerAsync(player).ConfigureAwait(false);
            if (i == 0)
            {
                await player.PlayerState.TryAdvanceToAsync(PlayerState.Disconnected).ConfigureAwait(false);
            }
        }

        var joiningPlayer = new TestPlayer(this._gameContext);
        joiningPlayer.SetIpAddress("127.0.0.1");
        await joiningPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);

        var eventArgs = new StateMachine.StateChangeEventArgs { NextState = PlayerState.Authenticated };

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that players with a different IP address don't count toward the connection limit.
    /// </summary>
    [Test]
    public async ValueTask DifferentIpDoesNotCountAsync()
    {
        // Arrange
        var player1 = new TestPlayer(this._gameContext);
        player1.SetIpAddress("127.0.0.1");
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player1).ConfigureAwait(false);

        var player2 = new TestPlayer(this._gameContext);
        player2.SetIpAddress("127.0.0.1");
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player2).ConfigureAwait(false);

        var player3 = new TestPlayer(this._gameContext);
        player3.SetIpAddress("127.0.0.1");
        await player3.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player3.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player3).ConfigureAwait(false);

        var joiningPlayer = new TestPlayer(this._gameContext);
        joiningPlayer.SetIpAddress(new System.Net.IPAddress(new byte[] { 192, 168, 1, 100 }).ToString());
        await joiningPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);

        var eventArgs = new StateMachine.StateChangeEventArgs { NextState = PlayerState.Authenticated };

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that players which are still at the login screen don't count toward the connection limit.
    /// </summary>
    [Test]
    public async ValueTask LoginScreenAndInitialPlayersDoNotCountAsync()
    {
        // Arrange
        var player1 = new TestPlayer(this._gameContext);
        player1.SetIpAddress("127.0.0.1");
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player1.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player1).ConfigureAwait(false);

        var player2 = new TestPlayer(this._gameContext);
        player2.SetIpAddress("127.0.0.1");
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player2.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player2).ConfigureAwait(false);

        // This player is only at the login screen
        var player3 = new TestPlayer(this._gameContext);
        player3.SetIpAddress("127.0.0.1");
        await player3.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player3).ConfigureAwait(false);

        var joiningPlayer = new TestPlayer(this._gameContext);
        joiningPlayer.SetIpAddress("127.0.0.1");
        await joiningPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);

        var eventArgs = new StateMachine.StateChangeEventArgs { NextState = PlayerState.Authenticated };

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that an offline session which was started from the same IP address counts toward the limit,
    /// so an account left in the offline mode (/offlevel) takes one of the places.
    /// </summary>
    [Test]
    public async ValueTask OfflineSessionOfSameIpCountsAsync()
    {
        // Arrange
        this._plugin.Configuration!.MaximumConnectionsPerIp = 2;
        await this.AddLoggedInPlayerAsync("127.0.0.1").ConfigureAwait(false);
        await AddOfflineSessionAsync(this._gameContext, "offline", "127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1", "other").ConfigureAwait(false);

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.True);
    }

    /// <summary>
    /// Tests that a login into the account of an offline session doesn't count that session,
    /// because the login takes it over.
    /// </summary>
    [Test]
    public async ValueTask LoginIntoAccountOfOfflineSessionSucceedsAsync()
    {
        // Arrange
        this._plugin.Configuration!.MaximumConnectionsPerIp = 2;
        await this.AddLoggedInPlayerAsync("127.0.0.1").ConfigureAwait(false);
        await AddOfflineSessionAsync(this._gameContext, "offline", "127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1", "OFFLINE").ConfigureAwait(false);

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that offline sessions of another IP address and offline sessions without a game client, like bots, don't count.
    /// </summary>
    [Test]
    public async ValueTask OfflineSessionsOfOtherIpAndBotsDoNotCountAsync()
    {
        // Arrange
        this._plugin.Configuration!.MaximumConnectionsPerIp = 2;
        await this.AddLoggedInPlayerAsync("127.0.0.1").ConfigureAwait(false);
        await AddOfflineSessionAsync(this._gameContext, "otherIp", "192.168.1.100").ConfigureAwait(false);
        await AddOfflineSessionAsync(this._gameContext, "bot", null).ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1", "other").ConfigureAwait(false);

        // Act
        await this._plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.False);
    }

    /// <summary>
    /// Tests that the sessions on the other game servers of the process count, too,
    /// when the plugin is created by the plugin manager with the game servers of the process.
    /// </summary>
    [Test]
    public async ValueTask SessionsOfOtherGameServersCountAsync()
    {
        // Arrange
        var otherContext = GameContextTestHelper.CreateGameContext();
        var otherPlayer = new TestPlayer(otherContext);
        otherPlayer.SetIpAddress("127.0.0.1");
        await otherPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await otherPlayer.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        var plugin = CreatePlugInOfProcess(CreateGameServer(otherPlayer));
        plugin.Configuration = new MaximumConnectionsPerIpPlugInConfiguration { MaximumConnectionsPerIp = 2 };

        await this.AddLoggedInPlayerAsync("127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1", "other").ConfigureAwait(false);

        // Act
        await plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.True);
    }

    /// <summary>
    /// Tests that the offline session of the account on another game server still counts,
    /// because only a login on the same game server takes it over.
    /// </summary>
    [Test]
    public async ValueTask OfflineSessionOfAccountOnOtherGameServerCountsAsync()
    {
        // Arrange
        var otherContext = (GameContext)GameContextTestHelper.CreateGameContext();
        var offlinePlayer = await AddOfflineSessionAsync(otherContext, "offline", "127.0.0.1").ConfigureAwait(false);
        var plugin = CreatePlugInOfProcess(CreateGameServer(offlinePlayer));
        plugin.Configuration = new MaximumConnectionsPerIpPlugInConfiguration { MaximumConnectionsPerIp = 2 };

        await this.AddLoggedInPlayerAsync("127.0.0.1").ConfigureAwait(false);
        var (joiningPlayer, eventArgs) = await this.CreateJoiningPlayerAsync("127.0.0.1", "offline").ConfigureAwait(false);

        // Act
        await plugin.PlayerStateChangingAsync(joiningPlayer, eventArgs).ConfigureAwait(false);

        // Assert
        Assert.That(eventArgs.Cancel, Is.True);
    }

    /// <summary>
    /// Tests that the plugin manager can create the plugin without the game servers of the process,
    /// e.g. when the game server runs in its own process.
    /// </summary>
    [Test]
    public void CanBeCreatedWithoutGameServersOfProcess()
    {
        Assert.That(() => ActivatorUtilities.CreateInstance<MaximumConnectionsPerIpPlugIn>(new ServiceContainer()), Throws.Nothing);
    }

    private static MaximumConnectionsPerIpPlugIn CreatePlugInOfProcess(IGameServer gameServer)
    {
        // Like the plugin manager, which creates the plugins with the services of the all-in-one host.
        var services = new ServiceContainer();
        services.AddService(typeof(ICollection<IGameServer>), new List<IGameServer> { gameServer });
        return ActivatorUtilities.CreateInstance<MaximumConnectionsPerIpPlugIn>(services);
    }

    private static IGameServer CreateGameServer(params Player[] players)
    {
        var context = new Mock<IGameServerContext>();
        context.Setup(c => c.GetPlayersAsync()).ReturnsAsync(players.ToList());
        var gameServer = new Mock<IGameServer>();
        gameServer.As<IGameServerContextProvider>().Setup(p => p.Context).Returns(context.Object);
        return gameServer.Object;
    }

    private static async ValueTask<OfflinePlayer> AddOfflineSessionAsync(GameContext gameContext, string loginName, string? clientIpAddress)
    {
        var offlinePlayer = new OfflinePlayer(gameContext)
        {
            Account = new Account { LoginName = loginName },
            ClientIpAddress = clientIpAddress,
        };
        await offlinePlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await offlinePlayer.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await gameContext.AddPlayerAsync(offlinePlayer).ConfigureAwait(false);
        Assert.That(gameContext.OfflinePlayerManager.TryAdd(loginName, offlinePlayer), Is.True);
        return offlinePlayer;
    }

    private async ValueTask AddLoggedInPlayerAsync(string ipAddress)
    {
        var player = new TestPlayer(this._gameContext);
        player.SetIpAddress(ipAddress);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        await this._gameContext.AddPlayerAsync(player).ConfigureAwait(false);
    }

    private async ValueTask<(TestPlayer Player, StateMachine.StateChangeEventArgs EventArgs)> CreateJoiningPlayerAsync(string ipAddress, string loginName)
    {
        var joiningPlayer = new TestPlayer(this._gameContext);
        joiningPlayer.SetIpAddress(ipAddress);
        await joiningPlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        joiningPlayer.LoginNameInProgress = loginName;
        return (joiningPlayer, new StateMachine.StateChangeEventArgs { NextState = PlayerState.Authenticated });
    }

    private class TestPlayer : Player, IHasIpAddress
    {
        private string? _ipAddress;

        public TestPlayer(IGameContext gameContext)
            : base(gameContext)
        {
        }

        /// <inheritdoc />
        public string? IpAddress => this._ipAddress;

        /// <summary>Sets the IP address for testing purposes.</summary>
        public void SetIpAddress(string? ip) => this._ipAddress = ip;

        protected override ICustomPlugInContainer<GameLogic.Views.IViewPlugIn> CreateViewPlugInContainer()
        {
            return new MockViewPlugInContainer();
        }
    }
}
