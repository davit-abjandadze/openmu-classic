// <copyright file="TeamVsTeamPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.TeamVsTeam;

using System.ComponentModel;
using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Team vs Team event: players register with /jointvt during the registration time,
/// then they are split into two balanced teams and warped to the battle map.
/// Kills of the other team don't make the killer a player killer, and players can't damage their own team.
/// When the time is over, the team with more kills wins: its best player (MVP) and the other members get points.
/// </summary>
[PlugIn]
[Display(Name = "Team vs Team", Description = "A scheduled battle of two balanced teams, joined with /jointvt. The winning team gets points.")]
[Guid("D4C7A1E3-2B5F-4A86-9C30-6E8F1B2D7A10")]
public class TeamVsTeamPlugIn : PeriodicTaskBasePlugIn<TeamVsTeamConfiguration, TeamVsTeamState>,
    ISupportDefaultCustomConfiguration,
    IDisabledByDefault,
    IPlayerKillPenaltyExemptionPlugIn,
    IPlayerAttackRestrictionPlugIn,
    IAttackableGotKilledPlugIn
{
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
        if (attacker == defender || !this.IsEventRunning(attacker.GameContext, out var state))
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
    /// <returns>The winning team and its best player, or <c>null</c> if it's a draw.</returns>
    internal static (int Team, Player MostValuablePlayer)? DetermineWinner(IReadOnlyDictionary<Player, int> teams, IReadOnlyDictionary<Player, int> kills)
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
            .OrderByDescending(p => kills.GetValueOrDefault(p))
            .First();
        return (winningTeam, mostValuablePlayer);
    }

    /// <inheritdoc />
    protected override TeamVsTeamState CreateState(IGameContext gameContext) => new(gameContext);

    /// <inheritdoc />
    protected override ValueTask OnPrepareEventAsync(TeamVsTeamState state)
    {
        lock (state.SyncRoot)
        {
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
            players = state.Registered.Where(IsAvailable).ToList();
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
            await player.WarpToAsync(gate).ConfigureAwait(false);
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

        var result = DetermineWinner(teams, kills);
        foreach (var (player, team) in teams)
        {
            if (!IsAvailable(player))
            {
                continue;
            }

            if (result is not { } winner)
            {
                await player.ShowLocalizedGoldenMessageAsync(nameof(PlayerMessage.TeamVsTeamDraw)).ConfigureAwait(false);
            }
            else
            {
                await player.ShowLocalizedGoldenMessageAsync(nameof(PlayerMessage.TeamVsTeamWinner), winner.Team + 1, winner.MostValuablePlayer.Name).ConfigureAwait(false);
                if (team == winner.Team && player.Attributes is { } attributes)
                {
                    var isMostValuablePlayer = player == winner.MostValuablePlayer;
                    var points = isMostValuablePlayer ? configuration.MostValuablePlayerPoints : configuration.WinnerPoints;
                    attributes[TeamVsTeamStats.Points] += points;
                    attributes[TeamVsTeamStats.Wins] += 1;
                    if (isMostValuablePlayer)
                    {
                        attributes[TeamVsTeamStats.MostValuablePlayer] += 1;
                    }

                    await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.TeamVsTeamPoints), points).ConfigureAwait(false);
                    await player.SaveProgressAsync().ConfigureAwait(false);
                }
            }

            if (player.CurrentMap?.Definition.Number == configuration.MapNumber)
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

    private static bool IsAvailable(Player player)
        => player.PlayerState.CurrentState == PlayerState.EnteredWorld && player.SelectedCharacter is not null;

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

    private bool AreEnemies(Player attacker, Player defender)
    {
        return attacker != defender
               && this.IsEventRunning(attacker.GameContext, out var state)
               && state.GetTeam(attacker) is { } attackerTeam
               && state.GetTeam(defender) is { } defenderTeam
               && attackerTeam != defenderTeam;
    }
}
