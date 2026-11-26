// <copyright file="OfflineSessionLoginServerTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests.Offline;

using System.Threading;
using Moq;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.Views.Login;
using MUnique.OpenMU.GameServer;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Tests that an offline session keeps its account reserved at the login server, which the game servers share.
/// Otherwise, another game server could log the account in while the session runs and load a second copy of
/// its characters, which would diverge from the session, e.g. duplicate items.
/// </summary>
[TestFixture]
public class OfflineSessionLoginServerTests
{
    private const string Password = "pw";

    private const string LoginName = "a";

    private readonly List<GameServerContext> _contexts = new();

    private ILoginServer _loginServer = null!;

    private Mock<IPlayerContext> _playerContext = null!;

    /// <summary>
    /// Sets up a fresh login server and the persistence mocks.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this._loginServer = new MUnique.OpenMU.LoginServer.LoginServer();
        this._playerContext = new Mock<IPlayerContext> { DefaultValue = DefaultValue.Mock };
        this._playerContext
            .Setup(c => c.AuthenticateAsync(It.IsAny<string>(), Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccountState.Normal);
        this._playerContext
            .Setup(c => c.GetAccountByLoginNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string loginName, CancellationToken _) => new Persistence.BasicModel.Account { LoginName = loginName });
    }

    /// <summary>
    /// Disposes the game server contexts of the test, which stops their timers.
    /// </summary>
    /// <returns>The task.</returns>
    [TearDown]
    public async Task TearDownAsync()
    {
        foreach (var context in this._contexts)
        {
            await context.DisposeAsync().ConfigureAwait(false);
        }

        this._contexts.Clear();
    }

    /// <summary>
    /// A login into the account on another game server is refused while the offline session runs.
    /// </summary>
    [Test]
    public async ValueTask LoginOnOtherGameServerIsRefusedAsync()
    {
        var sessionServer = this.CreateGameServerContext(0);
        var otherServer = this.CreateGameServerContext(1);
        await this.StartOfflineSessionAsync(sessionServer).ConfigureAwait(false);
        var player = await CreatePlayerAtLoginScreenAsync(otherServer).ConfigureAwait(false);

        await new LoginAction().LoginAsync(player, LoginName, Password).ConfigureAwait(false);

        VerifyLoginResult(player, LoginResult.AccountAlreadyConnected);
        Assert.That(sessionServer.OfflinePlayerManager.IsActive(LoginName), Is.True);
        Assert.That(await this.GetLoginServerIdAsync().ConfigureAwait(false), Is.EqualTo((byte)0));
    }

    /// <summary>
    /// A login into the account on the game server of the offline session takes the session over,
    /// together with the registration of the account at the login server.
    /// </summary>
    [Test]
    public async ValueTask LoginOnSameGameServerTakesOverSessionAsync()
    {
        var gameServer = this.CreateGameServerContext(0);
        await this.StartOfflineSessionAsync(gameServer).ConfigureAwait(false);
        var player = await CreatePlayerAtLoginScreenAsync(gameServer).ConfigureAwait(false);

        await new LoginAction().LoginAsync(player, LoginName, Password).ConfigureAwait(false);

        VerifyLoginResult(player, LoginResult.Ok);
        Assert.That(gameServer.OfflinePlayerManager.IsActive(LoginName), Is.False);
        Assert.That(await this.GetLoginServerIdAsync().ConfigureAwait(false), Is.EqualTo((byte)0));
        Assert.That(player.ReleaseLoginServerRegistration(), Is.EqualTo(LoginName), "The player holds the registration, so its disconnect logs the account off.");
    }

    /// <summary>
    /// When the offline session ended, it released the account, so it can be logged in on another game server.
    /// </summary>
    [Test]
    public async ValueTask EndedSessionReleasesAccountAsync()
    {
        var sessionServer = this.CreateGameServerContext(0);
        var otherServer = this.CreateGameServerContext(1);
        await this.StartOfflineSessionAsync(sessionServer).ConfigureAwait(false);

        await sessionServer.OfflinePlayerManager.StopAsync(LoginName).ConfigureAwait(false);

        Assert.That(await this.GetLoginServerIdAsync().ConfigureAwait(false), Is.Null);
        var player = await CreatePlayerAtLoginScreenAsync(otherServer).ConfigureAwait(false);
        await new LoginAction().LoginAsync(player, LoginName, Password).ConfigureAwait(false);
        VerifyLoginResult(player, LoginResult.Ok);
        Assert.That(await this.GetLoginServerIdAsync().ConfigureAwait(false), Is.EqualTo((byte)1));
    }

    /// <summary>
    /// An offline session which is disposed without a disconnect, e.g. because its start failed, releases the account, too.
    /// </summary>
    [Test]
    public async ValueTask SessionDisposedWithoutDisconnectReleasesAccountAsync()
    {
        var gameServer = this.CreateGameServerContext(0);
        var offlinePlayer = await this.StartOfflineSessionAsync(gameServer).ConfigureAwait(false);

        await offlinePlayer.DisposeAsync().ConfigureAwait(false);

        Assert.That(await this.GetLoginServerIdAsync().ConfigureAwait(false), Is.Null);
    }

    private static void VerifyLoginResult(Player player, LoginResult result)
    {
        Mock.Get(player.ViewPlugIns.GetPlugIn<IShowLoginResultPlugIn>()!)
            .Verify(p => p.ShowLoginResultAsync(result), Times.Once);
    }

    private static async ValueTask<Player> CreatePlayerAtLoginScreenAsync(IGameContext gameContext)
    {
        var player = new LoginTestPlayer(gameContext);
        await gameContext.AddPlayerAsync(player).ConfigureAwait(false);
        await player.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        return player;
    }

    /// <summary>
    /// Starts an offline session of the account which holds its registration at the login server,
    /// like a session which the <see cref="OfflinePlayerManager"/> started for a logged in player.
    /// </summary>
    /// <remarks>
    /// Starting a real offline session needs a player in the game with its account and character
    /// loaded, so it's added to the sessions of the manager directly.
    /// </remarks>
    private async ValueTask<OfflinePlayer> StartOfflineSessionAsync(GameServerContext gameContext)
    {
        Assert.That(await this._loginServer.TryLoginAsync(LoginName, gameContext.Id).ConfigureAwait(false), Is.True);
        var offlinePlayer = new OfflinePlayer(gameContext);
        offlinePlayer.SetLoginServerRegistration(LoginName);
        await gameContext.AddPlayerAsync(offlinePlayer).ConfigureAwait(false);
        await offlinePlayer.PlayerState.TryAdvanceToAsync(PlayerState.LoginScreen).ConfigureAwait(false);
        await offlinePlayer.PlayerState.TryAdvanceToAsync(PlayerState.Authenticated).ConfigureAwait(false);
        Assert.That(gameContext.OfflinePlayerManager.TryAdd(LoginName, offlinePlayer), Is.True);
        return offlinePlayer;
    }

    private async ValueTask<byte?> GetLoginServerIdAsync()
    {
        var snapshot = await this._loginServer.GetSnapshotAsync().ConfigureAwait(false);
        return snapshot.TryGetValue(LoginName, out var serverId) ? serverId : null;
    }

    private GameServerContext CreateGameServerContext(byte serverId)
    {
        var context = LoginTestSetup.CreateGameServerContext(LoginTestSetup.CreatePersistence(this._playerContext.Object), this._loginServer, serverId: serverId);
        this._contexts.Add(context);
        return context;
    }
}
