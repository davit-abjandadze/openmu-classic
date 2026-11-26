// <copyright file="ClassicRates.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// The rates of the classic server: experience, money and the drop chances.
/// </summary>
/// <remarks>
/// They are the rates which the server runs with, so a newly initialized database gets the same rates.
/// When a rate is changed in the admin panel for good, it should be changed here as well.
/// </remarks>
public static class ClassicRates
{
    /// <summary>
    /// The experience rate of the game configuration. The <see cref="GameLogic.Resets.ResetExperienceRatePlugIn"/> lowers it by the reset count.
    /// </summary>
    public const float ExperienceRate = 100;

    /// <summary>
    /// The money rate: dropped zen is multiplied with it.
    /// </summary>
    public const float MoneyAmountRate = 500;

    /// <summary>
    /// The drop chance of the common jewel drop group of all maps (2 %).
    /// </summary>
    public const double JewelDropChance = 0.02;

    /// <summary>
    /// The drop chance of the common excellent item drop group of all maps (1 %).
    /// </summary>
    public const double ExcellentDropChance = 0.01;

    /// <summary>
    /// The drop chance of the Loch's Feather in Icarus (5 %).
    /// </summary>
    public const double LochsFeatherDropChance = 0.05;

    /// <summary>
    /// The drop chance of the items of the second class quests, e.g. the Scroll of Emperor or the Broken Sword (50 %).
    /// </summary>
    public const double QuestItemDropChance = 0.5;

    /// <summary>
    /// The time how long a dropped item stays on the ground.
    /// </summary>
    public static readonly TimeSpan ItemDropDuration = TimeSpan.FromSeconds(50);

    private const short ExcellentDropGroupNumber = 3;

    private const short JewelsDropGroupNumber = 4;

    private const short LochsFeatherDropGroupNumber = 1;

    /// <summary>
    /// The drop groups of the quest items of the second class quests, as (quest number, item number, character class number),
    /// see the ids of the quest item drop groups in <see cref="Quests"/>.
    /// </summary>
    private static readonly (short Quest, short Item, byte CharacterClass)[] QuestItemDropGroups =
    [
        (0, 23, 0), // Scroll of Emperor (Dark Wizard)
        (0, 23, 4), // Scroll of Emperor (Dark Knight)
        (0, 23, 8), // Scroll of Emperor (Fairy Elf)
        (1, 24, 4), // Broken Sword (Dark Knight)
        (1, 25, 8), // Tear of Elf (Fairy Elf)
        (1, 26, 0), // Soul Shard of Wizard (Dark Wizard)
        (2, 23, 2), // Scroll of Emperor (Soul Master)
        (2, 23, 6), // Scroll of Emperor (Blade Knight)
        (2, 23, 10), // Scroll of Emperor (Muse Elf)
    ];

    /// <summary>
    /// Applies the rates to the game configuration.
    /// </summary>
    /// <param name="context">The context, which replaces the money rate.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The task.</returns>
    public static async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        gameConfiguration.ExperienceRate = ExperienceRate;
        gameConfiguration.PreventExperienceOverflow = true;
        gameConfiguration.ItemDropDuration = ItemDropDuration;

        // The value of an attribute can't be changed, so it's replaced.
        if (gameConfiguration.GlobalBaseAttributeValues.FirstOrDefault(a => a.Definition?.Id == Stats.MoneyAmountRate.Id) is { Definition: { } moneyRateDefinition } moneyRate
            && moneyRate.Value != MoneyAmountRate)
        {
            gameConfiguration.GlobalBaseAttributeValues.Remove(moneyRate);
            await context.DeleteAsync(moneyRate).ConfigureAwait(false);
            gameConfiguration.GlobalBaseAttributeValues.Add(context.CreateNew<ConstValueAttribute>(MoneyAmountRate, moneyRateDefinition, moneyRate.AggregateType));
        }

        SetChance(gameConfiguration, GuidHelper.CreateGuid<DropItemGroup>(JewelsDropGroupNumber), JewelDropChance);
        SetChance(gameConfiguration, GuidHelper.CreateGuid<DropItemGroup>(ExcellentDropGroupNumber), ExcellentDropChance);
        SetChance(gameConfiguration, GuidHelper.CreateGuid<DropItemGroup>(Version095d.Maps.Icarus.Number, LochsFeatherDropGroupNumber), LochsFeatherDropChance);
        foreach (var (quest, item, characterClass) in QuestItemDropGroups)
        {
            SetChance(gameConfiguration, GuidHelper.CreateGuid<DropItemGroup>(quest, item, characterClass), QuestItemDropChance);
        }
    }

    private static void SetChance(GameConfiguration gameConfiguration, Guid dropGroupId, double chance)
    {
        if (gameConfiguration.DropItemGroups.FirstOrDefault(g => g.GetId() == dropGroupId) is { } group)
        {
            group.Chance = chance;
        }
    }
}
