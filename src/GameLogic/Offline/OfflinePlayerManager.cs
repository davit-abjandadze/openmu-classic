// <copyright file="OfflinePlayerManager.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Offline;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MuHelper;
using MUnique.OpenMU.GameLogic.Views.Login;

/// <summary>
/// Manages active <see cref="OfflinePlayer"/> sessions.
/// </summary>
public sealed class OfflinePlayerManager
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, OfflinePlayer> _activePlayers =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a snapshot of all currently active offline players.
    /// </summary>
    public IReadOnlyCollection<OfflinePlayer> OfflinePlayers
        => this._activePlayers.Values.ToList();

    /// <summary>
    /// Starts an offline player session by replacing the real player with a copy.
    /// </summary>
    /// <param name="realPlayer">The real player who typed the command.</param>
    /// <param name="loginName">The pre-validated account login name.</param>
    /// <returns><c>true</c> if the offline session was started successfully.</returns>
    public async ValueTask<bool> StartAsync(Player realPlayer, string loginName)
    {
        var characterName = realPlayer.SelectedCharacter?.Name;

        if (string.IsNullOrEmpty(characterName))
        {
            return false;
        }

        var sentinel = new OfflinePlayer(realPlayer.GameContext)
        {
            ClientIpAddress = (realPlayer as IHasIpAddress)?.IpAddress,
        };

        // Atomically claim the slot to prevent racing during initialization.
        if (!this._activePlayers.TryAdd(loginName, sentinel))
        {
            await sentinel.DisposeAsync().ConfigureAwait(false);
            return false;
        }

        if (!this.TryChargeInitialZenCost(realPlayer))
        {
            await this.RemoveAndDisposeAsync(loginName, sentinel).ConfigureAwait(false);
            return false;
        }

        try
        {
            await this.TransitionToOfflineAsync(realPlayer, sentinel).ConfigureAwait(false);

            if (!await sentinel.InitializeAsync(loginName, characterName).ConfigureAwait(false))
            {
                await this.RemoveAndDisposeAsync(loginName, sentinel).ConfigureAwait(false);
                return false;
            }

            return true;
        }
        catch
        {
            await this.RemoveAndDisposeAsync(loginName, sentinel).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Stops and removes the offline session for the given account if one exists.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    public async ValueTask StopAsync(string loginName)
    {
        if (!this._activePlayers.TryRemove(loginName, out var offlinePlayer))
        {
            // The session might have been started and stopped outside the manager (e.g. in tests).
            return;
        }

        try
        {
            await offlinePlayer.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            offlinePlayer.Logger.LogError(ex, "Error stopping offline player session for {0}.", loginName);
        }
        finally
        {
            await offlinePlayer.GameContext.RemovePlayerAsync(offlinePlayer).ConfigureAwait(false);
            await offlinePlayer.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Returns whether an offline player session is currently active for <paramref name="loginName"/>.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    public bool IsActive(string loginName) => this._activePlayers.ContainsKey(loginName);

    /// <summary>
    /// Tries to get the active offline player for the given account login name.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="player">The offline player, if found.</param>
    /// <returns><c>true</c> if an active session exists; otherwise <c>false</c>.</returns>
    public bool TryGetPlayer(string loginName, out OfflinePlayer? player)
        => this._activePlayers.TryGetValue(loginName, out player);

    /// <summary>
    /// Adds an offline player session which was started outside of the manager.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="offlinePlayer">The offline player.</param>
    /// <returns><c>true</c> if the account had no active session yet.</returns>
    /// <remarks>
    /// For tests: a session which is started by <see cref="StartAsync"/> needs a player in the game,
    /// with its account and character loaded.
    /// </remarks>
    internal bool TryAdd(string loginName, OfflinePlayer offlinePlayer)
        => this._activePlayers.TryAdd(loginName, offlinePlayer);

    private async ValueTask TransitionToOfflineAsync(Player realPlayer, OfflinePlayer offlinePlayer)
    {
        // The offline session takes over the registration of the account at the login server, so no other game server
        // can log into the account while the session runs: the session saves its progress only when it ends, and a second
        // session of the account would diverge from it, e.g. duplicate items. A login on this game server takes the
        // session over (see LoginAction). It's taken over before the disconnect, so that the disconnect doesn't log off.
        if (realPlayer.ReleaseLoginServerRegistration() is { } loginName)
        {
            offlinePlayer.SetLoginServerRegistration(loginName);
        }

        // Send a close-game packet so the client exits cleanly without auto-reconnecting.
        // DisconnectAsync will then fire PlayerDisconnected, which triggers
        // RemovePlayerAsync (player list cleanup) and OnPlayerDisconnectedAsync (dispose).
        await realPlayer.InvokeViewPlugInAsync<ILogoutPlugIn>(p => p.LogoutAsync(LogoutType.CloseGame)).ConfigureAwait(false);

        await realPlayer.DisconnectAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Calculates and deducts the initial Zen cost for starting an offline player session.
    /// The cost is based on the first MuHelper cost stage multiplied by the player's total level.
    /// </summary>
    /// <param name="player">The player to charge.</param>
    /// <returns><c>true</c> if the cost was successfully charged or no cost applies; otherwise <c>false</c>.</returns>
    private bool TryChargeInitialZenCost(Player player)
    {
        var initialCost = this.CalculateInitialZenCost(player);

        return initialCost <= 0 || player.TryRemoveMoney(initialCost);
    }

    /// <summary>
    /// Calculates the initial Zen cost for the given player based on the MuHelper configuration
    /// and the player's combined normal and master level.
    /// </summary>
    /// <param name="player">The player for whom to calculate the cost.</param>
    /// <returns>The Zen amount to charge; 0 if no cost applies.</returns>
    private int CalculateInitialZenCost(Player player)
    {
        var config = player.GameContext.FeaturePlugIns.GetPlugIn<MuHelperFeaturePlugIn>()?.Configuration
                     ?? new MuHelperConfiguration();

        var costPerStage = config.CostPerStage.FirstOrDefault();
        if (costPerStage <= 0)
        {
            return 0;
        }

        var totalLevel = player.Level + (int)(player.Attributes?[Stats.MasterLevel] ?? 0);

        return costPerStage * totalLevel;
    }

    /// <summary>
    /// Removes the sentinel entry from the active players dictionary and disposes the ghost.
    /// Used when startup fails before the ghost is fully initialized. The ghost is already part of the
    /// game context when the failure happened after <see cref="Player.SetSelectedCharacterAsync"/> was
    /// reached, and it is never disconnected on this path - the disposal takes it out of the player
    /// list (see <see cref="Player.DisposeAsyncCore"/>) and logs the account off at the login server.
    /// </summary>
    /// <param name="loginName">The account login name.</param>
    /// <param name="sentinel">The ghost player to dispose.</param>
    private async ValueTask RemoveAndDisposeAsync(string loginName, OfflinePlayer sentinel)
    {
        this._activePlayers.TryRemove(loginName, out _);
        await sentinel.DisposeAsync().ConfigureAwait(false);
    }
}