// <copyright file="TeamVsTeamPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.TeamVsTeam;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Team vs Team event: players register with /jointvt during the registration time,
/// then they are split into two balanced teams and warped to the battle map.
/// On the battle map, kills of the other team don't make the killer a player killer, and players can't damage their own team.
/// When the time is over, the team with more kills wins: its best player (MVP) and the other members get silver medals,
/// see <see cref="MedalItems.AwardSilverAsync"/>.
/// </summary>
[PlugIn]
[Display(Name = "Team vs Team", Description = "A scheduled battle of two balanced teams, joined with /jointvt. The winning team gets silver medals.")]
[Guid("D4C7A1E3-2B5F-4A86-9C30-6E8F1B2D7A10")]
public class TeamVsTeamPlugIn : PeriodicTaskBasePlugIn<TeamVsTeamConfiguration, TeamVsTeamState>,
    ISupportDefaultCustomConfiguration,
    IDisabledByDefault,
    IPlayerKillPenaltyExemptionPlugIn,
    IPlayerAttackRestrictionPlugIn,
    IAttackableGotKilledPlugIn
{
    private static readonly CloseNpcDialogAction NpcDialogCloser = new();

    /// <summary>
    /// Gets the active instance of the plugin.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The active instance, or <c>null</c>.</returns>
    public static TeamVsTeamPlugIn? GetActive(IGameContext gameContext)
        => gameContext.PlugInManager.GetActivePlugInsOf<IAttackableGotKilledPlugIn>().OfType<TeamVsTeamPlugIn>().FirstOrDefault();

    /// <inheritdoc />
    public object CreateDefaultConfig() => TeamVsTeamConfiguration.Default;

    /// <summary>
    /// Tries to register the player for the next battle.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    public async ValueTask TryRegisterAsync(Player player)
    {
        var state = this.GetStateByGameContext(player.GameContext);
        if (state.State != PeriodicTaskState.Prepared)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TeamVsTeamNotOpen)).ConfigureAwait(false);
            return;
        }

        var run = state.Run;
        if (EventAccountsPerIpPlugIn.GetActive(player.GameContext) is { } accountsPerIp
            && !await accountsPerIp.TryAdmitAsync(player, typeof(TeamVsTeamPlugIn), run, () => state.Run == run && state.State is PeriodicTaskState.Prepared or PeriodicTaskState.Started).ConfigureAwait(false))
        {
            return;
        }

        int count;
        lock (state.SyncRoot)
        {
            if (state.Registered.Contains(player))
            {
                count = -1;
            }
            else
            {
                state.Registered.Add(player);
                count = state.Registered.Count;
            }
        }

        if (count < 0)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TeamVsTeamAlreadyRegistered)).ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TeamVsTeamRegistered), count).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void CheckExemption(Player attacker, Player defender, PlayerKillPenaltyExemptionArgs eventArgs)
    {
        if (this.AreEnemies(attacker, defender))
        {
            eventArgs.IsExempted = true;
        }
    }

    /// <inheritdoc />
    public void CheckAttack(Player attacker, Player defender, CancelEventArgs eventArgs)
    {
        if (attacker == defender
            || !this.IsEventRunning(attacker.GameContext, out var state)
            || !this.IsOnBattleMap(attacker)
            || !this.IsOnBattleMap(defender))
        {
            return;
        }

        if (state.GetTeam(attacker) is { } attackerTeam && state.GetTeam(defender) == attackerTeam)
        {
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is Player victim
            && (killer as Player ?? (killer as Monster)?.SummonedBy) is { } killerPlayer
            && this.AreEnemies(killerPlayer, victim)
            && this.IsEventRunning(killerPlayer.GameContext, out var state))
        {
            lock (state.SyncRoot)
            {
                state.Kills[killerPlayer] = state.Kills.GetValueOrDefault(killerPlayer) + 1;
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Splits the players into two teams of a similar strength.
    /// </summary>
    /// <param name="players">The players.</param>
    /// <returns>The team (0 or 1) of each player.</returns>
    internal static Dictionary<Player, int> SplitIntoTeams(IEnumerable<Player> players)
    {
        var teams = new Dictionary<Player, int>();
        var strengths = new float[2];
        foreach (var player in players.OrderByDescending(GetStrength))
        {
            var team = strengths[0] <= strengths[1] ? 0 : 1;
            teams[player] = team;
            strengths[team] += GetStrength(player);
        }

        return teams;
    }

    /// <summary>
    /// Determines the winning team and its best player.
    /// </summary>
    /// <param name="teams">The teams.</param>
    /// <param name="kills">The kills.</param>
    /// <param name="canBeMostValuablePlayer">Determines if a player can be the best player, e.g. because it's still in the game. All can be, if it's <c>null</c>.</param>
    /// <returns>The winning team and its best player, or <c>null</c> if it's a draw.
    /// The best player is <c>null</c>, if no player of the winning team can be it.</returns>
    /// <remarks>
    /// The kills of all players count for their team, even if they left the game in the meantime.
    /// </remarks>
    internal static (int Team, Player? MostValuablePlayer)? DetermineWinner(IReadOnlyDictionary<Player, int> teams, IReadOnlyDictionary<Player, int> kills, Func<Player, bool>? canBeMostValuablePlayer = null)
    {
        var teamKills = new int[2];
        foreach (var (player, team) in teams)
        {
            teamKills[team] += kills.GetValueOrDefault(player);
        }

        if (teamKills[0] == teamKills[1])
        {
            return null;
        }

        var winningTeam = teamKills[0] > teamKills[1] ? 0 : 1;
        var mostValuablePlayer = teams.Where(t => t.Value == winningTeam)
            .Select(t => t.Key)
            .Where(p => canBeMostValuablePlayer?.Invoke(p) ?? true)
            .OrderByDescending(p => kills.GetValueOrDefault(p))
            .FirstOrDefault();
        return (winningTeam, mostValuablePlayer);
    }

    /// <summary>
    /// Awards a player of the winning team: counts the win (and the MVP) and gives the silver medals.
    /// </summary>
    /// <param name="player">The player of the winning team.</param>
    /// <param name="isMostValuablePlayer">If set to <c>true</c>, the player is the best player (MVP) of the winning team.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The task.</returns>
    internal static async ValueTask AwardWinnerAsync(Player player, bool isMostValuablePlayer, TeamVsTeamConfiguration configuration)
    {
        if (player.Attributes is not { } attributes)
        {
            return;
        }

        attributes[TeamVsTeamStats.Wins] += 1;
        if (isMostValuablePlayer)
        {
            attributes[TeamVsTeamStats.MostValuablePlayer] += 1;
        }

        var silver = isMostValuablePlayer ? configuration.MostValuablePlayerSilverMedals : configuration.WinnerSilverMedals;
        await MedalItems.AwardSilverAsync(player, silver).ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the state of the event in the game context.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The state.</returns>
    internal TeamVsTeamState GetState(IGameContext gameContext) => this.GetStateByGameContext(gameContext);

    /// <inheritdoc />
    protected override TeamVsTeamState CreateState(IGameContext gameContext) => new(gameContext);

    /// <inheritdoc />
    protected override ValueTask OnPrepareEventAsync(TeamVsTeamState state)
    {
        lock (state.SyncRoot)
        {
            state.StartNewRun();
            state.Registered.Clear();
            state.Teams.Clear();
            state.Kills.Clear();
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask OnPreparedAsync(TeamVsTeamState state)
    {
        var minutes = (int)Math.Ceiling(this.Configuration?.PreStartMessageDelay.TotalMinutes ?? 0);
        await state.Context.ForEachPlayerAsync(player => player.ShowLocalizedGoldenMessageAsync(nameof(PlayerMessage.TeamVsTeamRegistrationOpen), minutes).AsTask()).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask OnStartedAsync(TeamVsTeamState state)
    {
        var configuration = this.Configuration ?? TeamVsTeamConfiguration.Default;
        List<Player> players;
        lock (state.SyncRoot)
        {
            // Registered players which are busy at the start (dead, trading, ...) still fight for their team,
            // they just can't be warped and have to come to the battle map on their own.
            players = state.Registered.Where(IsInGame).ToList();
            state.Registered.Clear();
        }

        if (players.Count < configuration.MinimumPlayers
            || state.Context.Configuration.Maps.FirstOrDefault(m => m.Number == configuration.MapNumber && m.IsActive) is not { } mapDefinition
            || mapDefinition.ExitGates.FirstOrDefault(g => g.IsSpawnGate) is not { } gate)
        {
            foreach (var player in players)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TeamVsTeamCancelled), configuration.MinimumPlayers).ConfigureAwait(false);
            }

            return;
        }

        var teams = SplitIntoTeams(players);
        lock (state.SyncRoot)
        {
            foreach (var (player, team) in teams)
            {
                state.Teams[player] = team;
            }
        }

        foreach (var (player, team) in teams)
        {
            if (await CanBeWarpedAsync(player).ConfigureAwait(false))
            {
                await player.WarpToAsync(gate).ConfigureAwait(false);
            }

            var teamMates = string.Join(", ", teams.Where(t => t.Value == team).Select(t => t.Key.Name));
            await player.ShowLocalizedGoldenMessageAsync(nameof(PlayerMessage.TeamVsTeamStarted), team + 1).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TeamVsTeamYourTeam), team + 1, teamMates).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    protected override async ValueTask OnFinishedAsync(TeamVsTeamState state)
    {
        var configuration = this.Configuration ?? TeamVsTeamConfiguration.Default;
        Dictionary<Player, int> teams;
        Dictionary<Player, int> kills;
        lock (state.SyncRoot)
        {
            teams = new Dictionary<Player, int>(state.Teams);
            kills = new Dictionary<Player, int>(state.Kills);
            state.Teams.Clear();
            state.Kills.Clear();
        }

        if (teams.Count == 0)
        {
            return;
        }

        // Players which are dead, trading etc. at the end still get their reward, only those who left the game don't.
        var result = DetermineWinner(teams, kills, IsInGame);
        foreach (var (player, team) in teams)
        {
            if (!IsInGame(player))
            {
                continue;
            }

            if (result is not { } winner)
            {
                await player.ShowLocalizedGoldenMessageAsync(nameof(PlayerMessage.TeamVsTeamDraw)).ConfigureAwait(false);
            }
            else
            {
                await player.ShowLocalizedGoldenMessageAsync(nameof(PlayerMessage.TeamVsTeamWinner), winner.Team + 1, winner.MostValuablePlayer?.Name ?? "-").ConfigureAwait(false);
                if (team == winner.Team)
                {
                    await AwardWinnerAsync(player, player == winner.MostValuablePlayer, configuration).ConfigureAwait(false);
                }
            }

            // A dead player respawns on the battle map and can leave it on its own.
            if (player.CurrentMap?.Definition.Number == configuration.MapNumber
                && await CanBeWarpedAsync(player).ConfigureAwait(false))
            {
                await WarpHomeAsync(player).ConfigureAwait(false);
            }
        }
    }

    private static float GetStrength(Player player)
    {
        var attributes = player.Attributes;
        return attributes is null ? 0 : attributes[Stats.Level] + (attributes[Stats.Resets] * player.GameContext.Configuration.MaximumLevel);
    }

    /// <summary>
    /// Determines whether the player is in the game with a character, in any state of it, e.g. also dead or trading.
    /// </summary>
    private static bool IsInGame(Player player)
        => player.SelectedCharacter is not null
           && player.PlayerState.CurrentState is var state
           && state.IsLoggedIn()
           && state != PlayerState.Authenticated
           && state != PlayerState.CharacterSelection;

    /// <summary>
    /// Determines whether the player can be warped by the event now. An open NPC dialog is closed for it;
    /// dead or trading players can't be warped.
    /// </summary>
    private static async ValueTask<bool> CanBeWarpedAsync(Player player)
    {
        if (player.PlayerState.CurrentState == PlayerState.NpcDialogOpened)
        {
            await NpcDialogCloser.CloseNpcDialogAsync(player).ConfigureAwait(false);
        }

        return player.PlayerState.CurrentState == PlayerState.EnteredWorld;
    }

    private static async ValueTask WarpHomeAsync(Player player)
    {
        if (player.SelectedCharacter?.CharacterClass?.HomeMap?.ExitGates.FirstOrDefault(g => g.IsSpawnGate) is { } homeGate)
        {
            await player.WarpToAsync(homeGate).ConfigureAwait(false);
        }
    }

    private bool IsEventRunning(IGameContext gameContext, out TeamVsTeamState state)
    {
        state = this.GetStateByGameContext(gameContext);
        return state.State == PeriodicTaskState.Started;
    }

    /// <summary>
    /// Determines whether the player is on the battle map. The teams only matter there,
    /// so that the event doesn't change the player killing rules on the other maps.
    /// </summary>
    private bool IsOnBattleMap(Player player)
        => player.CurrentMap?.Definition.Number == (this.Configuration?.MapNumber ?? TeamVsTeamConfiguration.Default.MapNumber);

    private bool AreEnemies(Player attacker, Player defender)
    {
        return attacker != defender
               && this.IsEventRunning(attacker.GameContext, out var state)
               && this.IsOnBattleMap(attacker)
               && this.IsOnBattleMap(defender)
               && state.GetTeam(attacker) is { } attackerTeam
               && state.GetTeam(defender) is { } defenderTeam
               && attackerTeam != defenderTeam;
    }
}
