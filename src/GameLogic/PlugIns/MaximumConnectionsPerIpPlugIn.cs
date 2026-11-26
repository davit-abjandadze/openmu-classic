// <copyright file="MaximumConnectionsPerIpPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A plugin that limits the number of accounts which are logged in at the same time from the same IP address.
/// </summary>
/// <remarks>
/// An offline session (<see cref="OfflinePlayer"/>, e.g. by the <c>/offlevel</c> command) counts for the IP address
/// of the game client which started it, so leaving accounts in the offline mode doesn't get around the limit.
/// A login into the account of an offline session on the same game server takes that session over, so it doesn't count
/// for that login. In the all-in-one deployment, the sessions of all game servers of the process count;
/// otherwise only the ones of the own game server.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.MaximumConnectionsPerIpPlugIn_Name), Description = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.MaximumConnectionsPerIpPlugIn_Description), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
[Guid("2C779F5E-379E-4CE0-BFCC-CA6455D757B3")]
public class MaximumConnectionsPerIpPlugIn : IPlayerStateChangingPlugIn, ISupportCustomConfiguration<MaximumConnectionsPerIpPlugInConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private readonly ICollection<IGameServer>? _gameServers;

    /// <summary>
    /// Initializes a new instance of the <see cref="MaximumConnectionsPerIpPlugIn"/> class,
    /// which only counts the sessions of the own game server.
    /// </summary>
    public MaximumConnectionsPerIpPlugIn()
        : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MaximumConnectionsPerIpPlugIn"/> class.
    /// </summary>
    /// <param name="gameServers">The game servers of this process, whose sessions count, too.
    /// It's available in the all-in-one deployment; otherwise, only the sessions of the own game server count.</param>
    [ActivatorUtilitiesConstructor]
    public MaximumConnectionsPerIpPlugIn(ICollection<IGameServer>? gameServers = null)
    {
        this._gameServers = gameServers;
    }

    /// <inheritdoc />
    public MaximumConnectionsPerIpPlugInConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public async ValueTask PlayerStateChangingAsync(Player player, StateMachine.StateChangeEventArgs eventArgs)
    {
        if (eventArgs.NextState != PlayerState.Authenticated)
        {
            return;
        }

        var ipAddress = (player as IHasIpAddress)?.IpAddress;
        if (string.IsNullOrEmpty(ipAddress))
        {
            return;
        }

        var config = this.Configuration ?? CreateDefaultConfiguration();
        var sameIpCount = 0;
        foreach (var gameContext in this.GetGameContexts(player.GameContext))
        {
            var players = await gameContext.GetPlayersAsync().ConfigureAwait(false);
            sameIpCount += players.Count(p =>
                p != player
                && GetIpAddress(p) == ipAddress
                && p.PlayerState.CurrentState.IsLoggedIn()
                && !IsTakenOverBy(p, player));
        }

        if (sameIpCount >= config.MaximumConnectionsPerIp)
        {
            player.Logger.LogWarning(
                "Login request for IP '{IpAddress}' was cancelled. It exceeded the maximum connection limit of {Limit}.",
                ipAddress,
                config.MaximumConnectionsPerIp);
            player.LoginResultOverride = Views.Login.LoginResult.ServerIsFull;
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig()
    {
        return CreateDefaultConfiguration();
    }

    private static MaximumConnectionsPerIpPlugInConfiguration CreateDefaultConfiguration()
    {
        return new MaximumConnectionsPerIpPlugInConfiguration();
    }

    private static string? GetIpAddress(Player player) => player switch
    {
        IHasIpAddress hasIpAddress => hasIpAddress.IpAddress,
        OfflinePlayer offlinePlayer => offlinePlayer.ClientIpAddress,
        _ => null,
    };

    /// <summary>
    /// Determines whether the player is the offline session which the login of <paramref name="loggingInPlayer"/> takes over.
    /// </summary>
    /// <remarks>
    /// The login only takes over the offline session of its account on its own game server (see the <see cref="PlayerActions.LoginAction"/>).
    /// A session on another game server keeps running, so it still counts.
    /// </remarks>
    private static bool IsTakenOverBy(Player player, Player loggingInPlayer)
        => player is OfflinePlayer offlinePlayer
           && offlinePlayer.GameContext == loggingInPlayer.GameContext
           && loggingInPlayer.LoginNameInProgress is { } loginName
           && string.Equals(offlinePlayer.AccountLoginName, loginName, StringComparison.OrdinalIgnoreCase);

    private List<IGameContext> GetGameContexts(IGameContext ownContext)
    {
        var contexts = this._gameServers?
            .OfType<IGameServerContextProvider>()
            .Select(provider => (IGameContext)provider.Context)
            .ToList() ?? [];
        if (!contexts.Contains(ownContext))
        {
            contexts.Add(ownContext);
        }

        return contexts;
    }
}
