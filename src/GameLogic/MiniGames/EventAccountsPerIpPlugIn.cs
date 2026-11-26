// <copyright file="EventAccountsPerIpPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Limits the accounts of one IP address which take part in the same event, e.g. the Devil Square, the Blood Castle
/// or the Team vs Team (by default to one). As soon as an account entered a run of an event, another account of the
/// same IP address can't enter it anymore, even after the first account left it again.
/// </summary>
/// <remarks>
/// <para>
/// All runs of an event which are running at the same time count together, e.g. the Devil Squares of all levels
/// and of all game servers: in the all-in-one deployment, the game servers of the process share the plugin instance.
/// Otherwise, only the own game server counts.
/// </para>
/// <para>
/// The same account may enter again, e.g. after it died or with another character.
/// Server-side bots have no IP address, so they're not limited.
/// The remembered entrants are lost when the plugin is recreated, e.g. after a change of its configuration.
/// </para>
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.EventAccountsPerIpPlugIn_Name), Description = nameof(PlugInResources.EventAccountsPerIpPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A872881A-B085-4966-A2D1-B4F2AA159EB0")]
public class EventAccountsPerIpPlugIn : IMiniGameEnteringPlugIn, ISupportCustomConfiguration<EventAccountsPerIpConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <summary>
    /// The entrants by the run of an event, e.g. by <see cref="MiniGameContext"/>.
    /// A run which isn't referenced anymore elsewhere is dropped together with its entrants.
    /// </summary>
    private readonly ConditionalWeakTable<object, EventRunEntrants> _runs = new();

    private readonly object _syncRoot = new();

    /// <inheritdoc />
    public EventAccountsPerIpConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the active instance of the plugin, which events other than the mini games use to admit their players.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The active instance, or <c>null</c>.</returns>
    /// <remarks>
    /// It's the instance of the plugin point, which the game servers of the process share.
    /// The feature plugins, in contrast, are created per game server.
    /// </remarks>
    public static EventAccountsPerIpPlugIn? GetActive(IGameContext gameContext)
        => gameContext.PlugInManager.GetActivePlugInsOf<IMiniGameEnteringPlugIn>().OfType<EventAccountsPerIpPlugIn>().FirstOrDefault();

    /// <inheritdoc />
    public object CreateDefaultConfig() => new EventAccountsPerIpConfiguration();

    /// <inheritdoc />
    public async ValueTask MiniGameEnteringAsync(Player player, MiniGameContext miniGame, CancelEventArgs eventArgs)
    {
        var isAdmitted = await this.TryAdmitAsync(
                player,
                miniGame.Definition.Type,
                miniGame,
                () => miniGame.State is MiniGameState.Open or MiniGameState.Closed or MiniGameState.Playing)
            .ConfigureAwait(false);
        if (!isAdmitted)
        {
            eventArgs.Cancel = true;
        }
    }

    /// <summary>
    /// Tries to admit the player to a run of an event. It fails, if the maximum number of other accounts of the
    /// IP address of the player already entered a running run of the same event; the player is then told why.
    /// Otherwise, the account of the player is remembered as an entrant of the run.
    /// </summary>
    /// <param name="player">The player which enters the event.</param>
    /// <param name="eventKey">The key of the event, which is equal for all of its runs, e.g. the <see cref="MiniGameType"/>.</param>
    /// <param name="run">The run of the event, e.g. the <see cref="MiniGameContext"/>.</param>
    /// <param name="isRunning">Determines whether the run is still running, so that its entrants count.
    /// It's called while holding a lock and must return immediately.</param>
    /// <returns><c>true</c>, if the player was admitted; otherwise, <c>false</c>.</returns>
    public async ValueTask<bool> TryAdmitAsync(Player player, object eventKey, object run, Func<bool> isRunning)
    {
        if ((player as IHasIpAddress)?.IpAddress is not { Length: > 0 } ipAddress
            || player.Account?.LoginName is not { Length: > 0 } loginName)
        {
            return true;
        }

        var maximumAccounts = Math.Max(1, this.Configuration?.MaximumAccountsPerIp ?? 1);
        HashSet<string> otherAccounts;
        lock (this._syncRoot)
        {
            otherAccounts = this.GetOtherAccounts(eventKey, ipAddress, loginName);
            if (otherAccounts.Count < maximumAccounts)
            {
                this._runs.GetValue(run, _ => new EventRunEntrants(eventKey, isRunning)).Add(ipAddress, loginName);
                return true;
            }
        }

        player.Logger.LogInformation(
            "Account '{LoginName}' wasn't admitted to {EventKey}, because the account(s) {OtherAccounts} of its IP address '{IpAddress}' already entered it.",
            loginName,
            eventKey,
            string.Join(", ", otherAccounts),
            ipAddress);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.EventEnteredByOtherAccountOfIp)).ConfigureAwait(false);
        return false;
    }

    private HashSet<string> GetOtherAccounts(object eventKey, string ipAddress, string loginName)
    {
        var accounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, entrants) in this._runs)
        {
            if (Equals(entrants.EventKey, eventKey) && entrants.IsRunning())
            {
                accounts.UnionWith(entrants.GetAccounts(ipAddress));
            }
        }

        accounts.Remove(loginName);
        return accounts;
    }

    /// <summary>
    /// The accounts which entered a run of an event, by their IP address.
    /// </summary>
    private sealed class EventRunEntrants
    {
        private readonly Dictionary<string, HashSet<string>> _accountsByIpAddress = new();

        private readonly Func<bool> _isRunning;

        public EventRunEntrants(object eventKey, Func<bool> isRunning)
        {
            this.EventKey = eventKey;
            this._isRunning = isRunning;
        }

        public object EventKey { get; }

        public bool IsRunning() => this._isRunning();

        public void Add(string ipAddress, string loginName)
        {
            if (!this._accountsByIpAddress.TryGetValue(ipAddress, out var accounts))
            {
                accounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                this._accountsByIpAddress.Add(ipAddress, accounts);
            }

            accounts.Add(loginName);
        }

        public IEnumerable<string> GetAccounts(string ipAddress)
            => this._accountsByIpAddress.TryGetValue(ipAddress, out var accounts) ? accounts : [];
    }
}
