// <copyright file="HuntingJewelPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives a player a jewel for each period of hunting time, up to a maximum per day.
/// The time between two monster kills counts as hunting time, up to a maximum, so idle time doesn't count.
/// </summary>
[PlugIn]
[Display(Name = "Jewels for hunting time", Description = "Gives a player a jewel for each period of hunting time (e.g. 20 minutes), up to a maximum per day.")]
[Guid("5A1E7C33-9D2B-4F6A-8E41-7B3C2D1F0A10")]
public class HuntingJewelPlugIn : IAttackableGotKilledPlugIn, ISupportCustomConfiguration<HuntingJewelConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private static readonly DateTime DayZero = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    private readonly LastKillTimes _lastKills = new();

    /// <inheritdoc />
    public HuntingJewelConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the active instance of the plugin.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The active instance, or <c>null</c>.</returns>
    public static HuntingJewelPlugIn? GetActive(IGameContext gameContext)
        => gameContext.PlugInManager.GetActivePlugInsOf<IAttackableGotKilledPlugIn>().OfType<HuntingJewelPlugIn>().FirstOrDefault();

    /// <summary>
    /// Gets the day number of the specified point in time, in the time zone of the server.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="utcNow">The point in time (UTC).</param>
    /// <returns>The day number.</returns>
    public static int GetDay(IGameContext gameContext, DateTime utcNow)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), gameContext.ServerTimeZone);
        return (int)(local.Date - DayZero).TotalDays;
    }

    /// <summary>
    /// Ensures that the number of jewels of the day belongs to the current day, and resets it otherwise.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="utcNow">The current point in time.</param>
    public static void EnsureCurrentDay(Player player, DateTime utcNow)
    {
        if (player.Attributes is not { } attributes)
        {
            return;
        }

        var day = GetDay(player.GameContext, utcNow);
        if ((int)attributes[HuntingJewelStats.Day] != day)
        {
            attributes[HuntingJewelStats.Day] = day;
            attributes[HuntingJewelStats.JewelsOfDay] = 0;
        }
    }

    /// <inheritdoc />
    public async ValueTask AttackableGotKilledAsync(IAttackable killed, IAttacker? killer)
    {
        if (killed is not Monster { SummonedBy: null } monster
            || monster.Definition.ObjectKind != NpcObjectKind.Monster
            || (killer as Player ?? (killer as Monster)?.SummonedBy) is not { } player
            || player.Attributes is not { } attributes
            || this.Configuration is not { } configuration
            || configuration.HuntingTimePerJewel <= TimeSpan.Zero)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await this.AddHuntingTimeAsync(player, attributes, configuration, now).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new HuntingJewelConfiguration();

    /// <summary>
    /// Adds the hunting time since the last kill of the player and gives a jewel, when enough time was collected.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="attributes">The attributes of the player.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="now">The current point in time.</param>
    internal async ValueTask AddHuntingTimeAsync(Player player, IAttributeSystem attributes, HuntingJewelConfiguration configuration, DateTime now)
    {
        if (player.SelectedCharacter is not { } character)
        {
            return;
        }

        var huntingTime = this._lastKills.GetTimeSinceLastKill(character, now);
        if (huntingTime > configuration.MaximumTimeBetweenKills)
        {
            huntingTime = configuration.MaximumTimeBetweenKills;
        }

        EnsureCurrentDay(player, now);
        if (attributes[HuntingJewelStats.JewelsOfDay] >= configuration.MaximumJewelsPerDay)
        {
            return;
        }

        var seconds = attributes[HuntingJewelStats.HuntingSeconds] + (float)huntingTime.TotalSeconds;
        var required = (float)configuration.HuntingTimePerJewel.TotalSeconds;
        attributes[HuntingJewelStats.HuntingSeconds] = Math.Min(seconds, required);
        if (seconds < required
            || GetRandomJewel(player, configuration) is not { } jewelDefinition)
        {
            return;
        }

        var item = await TryAddJewelAsync(player, jewelDefinition).ConfigureAwait(false);
        if (item is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.HuntingJewelInventoryFull)).ConfigureAwait(false);
            return;
        }

        attributes[HuntingJewelStats.HuntingSeconds] = 0;
        attributes[HuntingJewelStats.JewelsOfDay]++;
        await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.HuntingJewelEarned),
                jewelDefinition.Name.GetTranslation(player.Culture),
                (int)attributes[HuntingJewelStats.JewelsOfDay],
                configuration.MaximumJewelsPerDay)
            .ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
    }

    private static ItemDefinition? GetRandomJewel(Player player, HuntingJewelConfiguration configuration)
    {
        var candidates = new List<ItemDefinition>();
        foreach (var key in configuration.Jewels)
        {
            var parts = key.Split('/');
            if (parts.Length == 2
                && byte.TryParse(parts[0], out var group)
                && short.TryParse(parts[1], out var number)
                && player.GameContext.Configuration.Items.FirstOrDefault(i => i.Group == group && i.Number == number && i.IsActive) is { } definition)
            {
                candidates.Add(definition);
            }
        }

        return candidates.Count == 0 ? null : candidates[Rand.NextInt(0, candidates.Count)];
    }

    private static async ValueTask<Item?> TryAddJewelAsync(Player player, ItemDefinition definition)
    {
        return await player.RunPersistenceExclusiveAsync(async () =>
        {
            if (player.Inventory is not { } inventory)
            {
                return null;
            }

            var item = player.PersistenceContext.CreateNew<Item>();
            item.Definition = definition;
            item.Durability = definition.Durability;
            if (await inventory.AddItemAsync(item).ConfigureAwait(false))
            {
                return item;
            }

            await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
            return (Item?)null;
        }).ConfigureAwait(false);
    }
}
