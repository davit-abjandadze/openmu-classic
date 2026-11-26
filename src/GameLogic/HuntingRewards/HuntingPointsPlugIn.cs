// <copyright file="HuntingPointsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives hunting points from a minimum level on: one point per period of hunting time, up to a maximum hunting time per day.
/// The time between two monster kills counts as hunting time, up to a maximum, so idle time doesn't count.
/// The points can be exchanged with the /hunt command.
/// </summary>
[PlugIn]
[Display(Name = "Hunting points", Description = "Gives hunting points from a minimum level on, one per minute of hunting up to a maximum per day, which can be exchanged with /hunt.")]
[Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A30")]
public class HuntingPointsPlugIn : IAttackableGotKilledPlugIn, ISupportCustomConfiguration<HuntingPointsConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private readonly LastKillTimes _lastKills = new();

    /// <inheritdoc />
    public HuntingPointsConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the active instance of the plugin.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The active instance, or <c>null</c>.</returns>
    public static HuntingPointsPlugIn? GetActive(IGameContext gameContext)
        => gameContext.PlugInManager.GetActivePlugInsOf<IAttackableGotKilledPlugIn>().OfType<HuntingPointsPlugIn>().FirstOrDefault();

    /// <summary>
    /// Ensures that the hunting seconds of the day belong to the current day, and resets them otherwise.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="utcNow">The current point in time.</param>
    public static void EnsureCurrentDay(Player player, DateTime utcNow)
    {
        if (player.Attributes is not { } attributes)
        {
            return;
        }

        var day = HuntingJewelPlugIn.GetDay(player.GameContext, utcNow);
        if ((int)attributes[HuntingPointStats.Day] != day)
        {
            attributes[HuntingPointStats.Day] = day;
            attributes[HuntingPointStats.SecondsOfDay] = 0;
        }
    }

    /// <inheritdoc />
    public ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is Monster { SummonedBy: null } monster
            && monster.Definition.ObjectKind == NpcObjectKind.Monster
            && (killer as Player ?? (killer as Monster)?.SummonedBy) is { Attributes: { } attributes } player
            && this.Configuration is { } configuration)
        {
            this.AddHuntingTime(player, attributes, configuration, DateTime.UtcNow);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => HuntingPointsConfiguration.CreateDefault();

    /// <summary>
    /// Adds the hunting time since the last kill of the player and converts it into points.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="attributes">The attributes of the player.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="now">The current point in time.</param>
    internal void AddHuntingTime(Player player, IAttributeSystem attributes, HuntingPointsConfiguration configuration, DateTime now)
    {
        if (player.SelectedCharacter is not { } character)
        {
            return;
        }

        var huntingTime = this._lastKills.GetTimeSinceLastKill(character, now);
        if (attributes[Stats.Level] < configuration.MinimumLevel || configuration.HuntingTimePerPoint <= TimeSpan.Zero)
        {
            return;
        }

        if (huntingTime > configuration.MaximumTimeBetweenKills)
        {
            huntingTime = configuration.MaximumTimeBetweenKills;
        }

        EnsureCurrentDay(player, now);
        var remainingOfDay = (float)configuration.MaximumHuntingTimePerDay.TotalSeconds - attributes[HuntingPointStats.SecondsOfDay];
        var seconds = Math.Min((float)huntingTime.TotalSeconds, Math.Max(0, remainingOfDay));
        if (seconds <= 0)
        {
            return;
        }

        attributes[HuntingPointStats.SecondsOfDay] += seconds;
        var pending = attributes[HuntingPointStats.PendingSeconds] + seconds;
        var secondsPerPoint = (float)configuration.HuntingTimePerPoint.TotalSeconds;
        var points = (int)(pending / secondsPerPoint);
        attributes[HuntingPointStats.PendingSeconds] = pending - (points * secondsPerPoint);
        if (points > 0)
        {
            attributes[HuntingPointStats.Points] += points;
        }
    }
}
