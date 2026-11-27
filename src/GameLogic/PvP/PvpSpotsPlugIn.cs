// <copyright file="PvpSpotsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PvP;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Offline;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// PvP spots: areas in which killing another player never makes the killer a player killer and earns prestige.
/// Killing a player killer earns (more) prestige everywhere, and so does winning a duel.
/// Kills, deaths and assists are counted per week and in total, see <see cref="PvpStats"/>.
/// </summary>
[PlugIn]
[Display(Name = "PvP spots and prestige", Description = "Areas in which killing players doesn't make the killer a player killer and earns prestige. Kills, deaths and assists are counted per week and in total.")]
[Guid("2D7E5B1A-4F3C-4E8B-A6D2-9C1F0E3B7A51")]
public class PvpSpotsPlugIn : IPlayerKillPenaltyExemptionPlugIn, IAttackableGotHitPlugIn, IAttackableGotKilledPlugIn, IAttackableMovedPlugIn, IObjectAddedToMapPlugIn, IObjectRemovedFromMapPlugIn, IDuelWonPlugIn, ISupportCustomConfiguration<PvpSpotsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <summary>
    /// The recent hits on a victim per attacker, to determine the assistants of a kill.
    /// A weak table, so that it doesn't keep victims alive which left the game without dying.
    /// </summary>
    private readonly ConditionalWeakTable<Player, ConcurrentDictionary<Player, DateTime>> _recentHits = new();

    private readonly ConcurrentDictionary<(Guid Killer, Guid Victim), List<DateTime>> _recentKills = new();
    private readonly ConditionalWeakTable<Player, ZonePresence> _zonePresence = new();

    private long _nextRecentKillsCleanupTicks;

    /// <inheritdoc />
    public PvpSpotsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the active instance of the plugin.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The active instance, or <c>null</c>.</returns>
    public static PvpSpotsPlugIn? GetActive(IGameContext gameContext)
        => gameContext.PlugInManager.GetActivePlugInsOf<IAttackableGotKilledPlugIn>().OfType<PvpSpotsPlugIn>().FirstOrDefault();

    /// <summary>
    /// Ensures that the weekly values of the player belong to the current week, and resets them otherwise.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="utcNow">The current point in time.</param>
    public static void EnsureCurrentWeek(Player player, PvpSpotsConfiguration configuration, DateTime utcNow)
    {
        if (player.Attributes is not { } attributes)
        {
            return;
        }

        var week = configuration.GetWeekNumber(utcNow);
        if ((int)attributes[PvpStats.Week] == week)
        {
            return;
        }

        attributes[PvpStats.Week] = week;
        foreach (var attribute in PvpStats.Weekly)
        {
            attributes[attribute] = 0;
        }
    }

    /// <summary>
    /// Determines whether the player is in a PvP spot.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The spot, or <c>null</c>.</returns>
    public static PvpSpotsConfiguration.PvpSpot? GetSpotOf(Player player, PvpSpotsConfiguration configuration)
    {
        if (player.CurrentMap is not { } map || player.IsAtSafezone())
        {
            return null;
        }

        return configuration.GetSpot(map.Definition.Number, player.Position.X, player.Position.Y);
    }

    /// <inheritdoc />
    public void CheckExemption(Player attacker, Player defender, PlayerKillPenaltyExemptionArgs eventArgs)
    {
        if (this.Configuration is { } configuration
            && attacker.CurrentMap is { } map
            && map == defender.CurrentMap
            && GetSpotOf(attacker, configuration) is not null
            && GetSpotOf(defender, configuration) is not null)
        {
            eventArgs.IsExempted = true;
        }
    }

    /// <inheritdoc />
    public void AttackableMoved(IAttackable attackable)
    {
        if (attackable is Player player && this._zonePresence.TryGetValue(player, out var presence))
        {
            _ = this.NotifyZoneChangeAsync(player, presence);
        }
    }

    /// <inheritdoc />
    public ValueTask ObjectAddedToMapAsync(GameMap map, ILocateable addedObject)
    {
        if (addedObject is not Player player || player is OfflinePlayer)
        {
            return ValueTask.CompletedTask;
        }

        var presence = this._zonePresence.GetValue(player, static _ => new ZonePresence());
        return this.NotifyZoneChangeAsync(player, presence, map);
    }

    /// <inheritdoc />
    public ValueTask ObjectRemovedFromMapAsync(GameMap map, ILocateable removedObject)
    {
        if (removedObject is Player player && this._zonePresence.TryGetValue(player, out var presence))
        {
            lock (presence)
            {
                if (presence.Map == map)
                {
                    // Keep the last spot until the next arrival so a map change can announce an exit.
                    presence.Map = null;
                }
            }
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public void AttackableGotHit(IAttackable attackable, IAttacker attacker, HitInfo hitInfo)
    {
        if (attackable is not Player victim
            || (attacker as Player ?? (attacker as Monster)?.SummonedBy) is not { } attackerPlayer
            || attackerPlayer == victim)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hits = this._recentHits.GetValue(victim, _ => new ConcurrentDictionary<Player, DateTime>());
        hits[attackerPlayer] = now;

        // Hits older than the assist time don't count anymore. Removing them keeps attackers
        // from being referenced after they left the game, as long as the victim stays alive.
        var assistTime = this.Configuration?.AssistTime ?? TimeSpan.Zero;
        foreach (var hit in hits)
        {
            if (hit.Value + assistTime < now)
            {
                hits.TryRemove(hit);
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not Player victim)
        {
            return;
        }

        this._recentHits.TryGetValue(victim, out var hits);
        this._recentHits.Remove(victim);
        if ((killer as Player ?? (killer as Monster)?.SummonedBy) is not { } killerPlayer
            || killerPlayer == victim
            || killerPlayer.DuelRoom is not null
            || this.Configuration is not { } configuration
            || killerPlayer.SelectedCharacter is not { } killerCharacter
            || victim.SelectedCharacter is not { } victimCharacter
            || killerPlayer.Attributes is null
            || victim.Attributes is null
            || !CountsAsOpponent(killerPlayer, victim, configuration))
        {
            return;
        }

        // Like the exemption of the player killer penalty, a spot kill requires both players in a spot.
        var isInSpot = GetSpotOf(victim, configuration) is not null && GetSpotOf(killerPlayer, configuration) is not null;
        var isPlayerKillerVictim = victimCharacter.State >= HeroState.PlayerKiller1stStage;
        if (!isInSpot && !isPlayerKillerVictim)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var prestige = this.IsFarming(killerCharacter.Id, victimCharacter.Id, configuration, now)
            ? 0
            : CalculatePrestige(killerPlayer, victim, isPlayerKillerVictim, configuration);

        EnsureCurrentWeek(killerPlayer, configuration, now);
        EnsureCurrentWeek(victim, configuration, now);
        AddKill(killerPlayer, prestige);
        AddDeath(victim);

        var assistants = hits?
            .Where(hit => hit.Key != killerPlayer
                          && hit.Value + configuration.AssistTime >= now
                          && hit.Key.CurrentMap == victim.CurrentMap
                          && hit.Key.Attributes is not null)
            .Select(hit => hit.Key)
            .ToList() ?? [];
        foreach (var assistant in assistants)
        {
            EnsureCurrentWeek(assistant, configuration, now);
            assistant.Attributes![PvpStats.WeeklyAssists]++;
            assistant.Attributes[PvpStats.TotalAssists]++;
        }

        if (prestige > 0)
        {
            await killerPlayer.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpPrestigeGained), prestige, (int)killerPlayer.Attributes[PvpStats.WeeklyPrestige]).ConfigureAwait(false);
        }
        else
        {
            await killerPlayer.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpNoPrestigeSameVictim), victimCharacter.Name).ConfigureAwait(false);
        }

        await killerPlayer.SaveProgressAsync().ConfigureAwait(false);
        await victim.SaveProgressAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DuelWonAsync(Player winner, Player loser)
    {
        if (this.Configuration is not { PrestigePerDuelVictory: > 0 } configuration
            || winner.SelectedCharacter is not { } winnerCharacter
            || loser.SelectedCharacter is not { } loserCharacter
            || winner.Attributes is not { } attributes
            || loser.Attributes is null
            || !CountsAsOpponent(winner, loser, configuration))
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (this.IsFarming(winnerCharacter.Id, loserCharacter.Id, configuration, now))
        {
            await winner.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpNoPrestigeSameVictim), loserCharacter.Name).ConfigureAwait(false);
            return;
        }

        EnsureCurrentWeek(winner, configuration, now);
        attributes[PvpStats.WeeklyPrestige] += configuration.PrestigePerDuelVictory;
        attributes[PvpStats.TotalPrestige] += configuration.PrestigePerDuelVictory;
        await winner.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.PvpPrestigeGained), configuration.PrestigePerDuelVictory, (int)attributes[PvpStats.WeeklyPrestige]).ConfigureAwait(false);
        await winner.SaveProgressAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new PvpSpotsConfiguration();

    /// <summary>
    /// Calculates the prestige for a kill.
    /// </summary>
    /// <param name="killer">The killer.</param>
    /// <param name="victim">The victim.</param>
    /// <param name="isPlayerKillerVictim">If set to <c>true</c>, the victim is a player killer.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The prestige.</returns>
    internal static int CalculatePrestige(Player killer, Player victim, bool isPlayerKillerVictim, PvpSpotsConfiguration configuration)
    {
        float prestige = configuration.PrestigePerKill;
        if (GetStrength(victim) - GetStrength(killer) > configuration.StrongerOpponentLevelDifference)
        {
            prestige *= configuration.StrongerOpponentMultiplier;
        }

        if (isPlayerKillerVictim)
        {
            prestige *= configuration.PlayerKillerVictimMultiplier;
        }

        return (int)Math.Round(prestige);
    }

    /// <summary>
    /// Determines whether the victim counts as an opponent of the killer, for the statistics and the prestige.
    /// Members of the same party and characters below the <see cref="PvpSpotsConfiguration.MinimumVictimLevel"/>
    /// without resets don't count, so that they can't be farmed for prestige.
    /// </summary>
    /// <param name="killer">The killer.</param>
    /// <param name="victim">The victim.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns><c>true</c>, if the victim counts as an opponent.</returns>
    internal static bool CountsAsOpponent(Player killer, Player victim, PvpSpotsConfiguration configuration)
    {
        if (killer.Party is { } party && ReferenceEquals(party, victim.Party))
        {
            return false;
        }

        return victim.Attributes is { } attributes
               && (attributes[Stats.Level] >= configuration.MinimumVictimLevel || attributes[Stats.Resets] > 0);
    }

    private static float GetStrength(Player player)
    {
        var attributes = player.Attributes!;
        return attributes[Stats.Level] + (attributes[Stats.Resets] * player.GameContext.Configuration.MaximumLevel);
    }

    private static void AddKill(Player killer, int prestige)
    {
        var attributes = killer.Attributes!;
        attributes[PvpStats.WeeklyKills]++;
        attributes[PvpStats.TotalKills]++;
        attributes[PvpStats.WeeklyPrestige] += prestige;
        attributes[PvpStats.TotalPrestige] += prestige;
    }

    private static void AddDeath(Player victim)
    {
        var attributes = victim.Attributes!;
        attributes[PvpStats.WeeklyDeaths]++;
        attributes[PvpStats.TotalDeaths]++;
    }

    private async ValueTask NotifyZoneChangeAsync(Player player, ZonePresence presence, GameMap? arrivalMap = null)
    {
        string messageKey;
        lock (presence)
        {
            var map = arrivalMap ?? presence.Map;
            if (map is null || player.CurrentMap != map)
            {
                return;
            }

            var spot = this.Configuration is { } configuration ? GetSpotOf(player, configuration) : null;
            if (ReferenceEquals(spot, presence.Spot) && (spot is null || presence.Map == map))
            {
                presence.Map = map;
                return;
            }

            presence.Map = map;
            presence.Spot = spot;
            messageKey = spot is null ? nameof(PlayerMessage.LeftPvpZone) : nameof(PlayerMessage.EnteredPvpZone);
        }

        // Movement notifications are synchronous. A failed client send must never interrupt movement.
        try
        {
            await player.ShowLocalizedBlueMessageAsync(messageKey).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            player.Logger.LogDebug(ex, "Couldn't send the PvP zone notification to {Player}", player.Name);
        }
    }

    private bool IsFarming(Guid killerId, Guid victimId, PvpSpotsConfiguration configuration, DateTime now)
    {
        if (configuration.MaximumKillsOfSameVictim <= 0)
        {
            return false;
        }

        this.RemoveExpiredKills(configuration, now);
        var key = (killerId, victimId);
        while (true)
        {
            var kills = this._recentKills.GetOrAdd(key, _ => []);
            lock (kills)
            {
                if (!this._recentKills.TryGetValue(key, out var current) || !ReferenceEquals(current, kills))
                {
                    // The list was removed by the cleanup in the meantime.
                    continue;
                }

                kills.RemoveAll(time => time + configuration.SameVictimTime < now);
                kills.Add(now);
                return kills.Count > configuration.MaximumKillsOfSameVictim;
            }
        }
    }

    /// <summary>
    /// Removes the expired kills of all pairs, at most once per <see cref="PvpSpotsConfiguration.SameVictimTime"/>,
    /// so that the remembered pairs don't grow endlessly.
    /// </summary>
    private void RemoveExpiredKills(PvpSpotsConfiguration configuration, DateTime now)
    {
        var next = Interlocked.Read(ref this._nextRecentKillsCleanupTicks);
        if (now.Ticks < next
            || Interlocked.CompareExchange(ref this._nextRecentKillsCleanupTicks, (now + configuration.SameVictimTime).Ticks, next) != next)
        {
            return;
        }

        foreach (var (key, kills) in this._recentKills)
        {
            lock (kills)
            {
                kills.RemoveAll(time => time + configuration.SameVictimTime < now);
                if (kills.Count == 0)
                {
                    this._recentKills.TryRemove(new KeyValuePair<(Guid Killer, Guid Victim), List<DateTime>>(key, kills));
                }
            }
        }
    }

    private sealed class ZonePresence
    {
        public GameMap? Map { get; set; }

        public PvpSpotsConfiguration.PvpSpot? Spot { get; set; }
    }
}
