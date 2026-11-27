// <copyright file="ClassicBoxes.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.Initialization.Properties;

/// <summary>
/// Adapts the boxes of the classic server:
/// <list type="bullet">
///   <item>The Box of Heaven (Box of Luck +7) always drops a normal (not excellent) weapon or armor piece
///     of <see cref="BoxOfHeavenMinimumLevel"/> to <see cref="BoxOfHeavenMaximumLevel"/>, instead of a jewel or zen.
///     Equipment is capped at the drop level of Dragon Armor.</item>
///   <item>Monsters of at least level <see cref="BoxOfHeavenMinimumMonsterLevel"/> drop the Box of Heaven with a chance of
///     <see cref="BoxOfHeavenDropChance"/>, on all maps which have the common jewel drop.</item>
///   <item>Caren the Barmaid sells the Box of Kundun +1 to +4 (Box of Luck +8 to +11).</item>
///   <item>The excellent Archangel weapons drop from the Box of Kundun +5 instead of the +4. They are the strongest weapons
///     of the classic server, but the other weapons of the +5 are either weaker or not part of it.</item>
///   <item>The Box of Kundun +5 is held back: nothing sells or drops it, see <see cref="HighestBoxOfKundunLevel"/>.</item>
/// </list>
/// </summary>
/// <remarks>
/// It depends on the active state of the items, so it has to be applied after the <see cref="ClassicContentRestriction"/>.
/// </remarks>
public static class ClassicBoxes
{
    /// <summary>
    /// The minimum level of the items which drop from the Box of Heaven.
    /// </summary>
    public const byte BoxOfHeavenMinimumLevel = 7;

    /// <summary>
    /// The maximum level of the items which drop from the Box of Heaven.
    /// </summary>
    public const byte BoxOfHeavenMaximumLevel = 11;

    /// <summary>
    /// The level of the Box of Luck which is the Box of Heaven.
    /// </summary>
    public const byte BoxOfHeavenLevel = 7;

    /// <summary>
    /// The level of the Box of Luck which is the Box of Kundun +1. The Box of Kundun +5 is four levels above.
    /// </summary>
    public const byte FirstBoxOfKundunLevel = 8;

    /// <summary>
    /// The level of the Box of Luck which is the Box of Kundun +5.
    /// </summary>
    public const byte LastBoxOfKundunLevel = 12;

    /// <summary>
    /// The level of the Box of Luck which is the Box of Kundun +4, from which the Archangel weapons are moved away.
    /// </summary>
    public const byte BoxOfKundunPlus4Level = 11;

    /// <summary>
    /// The level of the Box of Luck which is the highest Box of Kundun in the game, the +4.
    /// </summary>
    /// <remarks>
    /// The Box of Kundun +5 with the Archangel weapons is held back in the first time of the server, so that the
    /// characters develop with the lower boxes first. Caren the Barmaid doesn't sell it, and the monsters which drop it
    /// in the original data (the Golden Tantalos of the Golden Invasion) drop the +4 instead. It's added later.
    /// </remarks>
    public const byte HighestBoxOfKundunLevel = BoxOfKundunPlus4Level;

    /// <summary>
    /// The chance that a monster drops the Box of Heaven (1 %).
    /// </summary>
    public const double BoxOfHeavenDropChance = 0.01;

    /// <summary>
    /// The minimum level of the monsters which drop the Box of Heaven, so that it doesn't drop in the beginner areas.
    /// </summary>
    public const byte BoxOfHeavenMinimumMonsterLevel = 30;

    /// <summary>
    /// The Archangel weapons (group, number) which drop from the Box of Kundun +5 instead of the +4.
    /// </summary>
    public static readonly IReadOnlyList<(byte Group, short Number)> ArchangelWeapons =
    [
        (0, 19), // Divine Sword of Archangel
        (2, 13), // Divine Scepter of Archangel
        (4, 18), // Divine Crossbow of Archangel
        (5, 10), // Divine Staff of Archangel
    ];

    /// <summary>
    /// The highest drop level among the Dragon, Legendary and Guardian sets (Dragon Armor).
    /// </summary>
    internal const byte BoxOfHeavenMaximumDropLevel = 59;

    /// <summary>
    /// The number of the common jewel drop group (see <see cref="GameConfigurationInitializerBase"/>),
    /// whose maps also get the Box of Heaven drop.
    /// </summary>
    private const short JewelsDropGroupNumber = 4;

    private const byte BoxOfLuckGroup = 14;

    private const short BoxOfLuckNumber = 11;

    private const short CarenTheBarmaidNumber = 244;

    /// <summary>
    /// The highest item group of equipment: groups 0 to 6 are weapons and shields, groups 7 to 11 armor pieces.
    /// </summary>
    private const byte LastEquipmentGroup = 11;

    /// <summary>
    /// Applies the box changes. It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The task.</returns>
    public static async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (GetBoxOfLuck(gameConfiguration) is not { } boxOfLuck)
        {
            return;
        }

        await ApplyBoxOfHeavenAsync(context, gameConfiguration, boxOfLuck).ConfigureAwait(false);
        ApplyBoxOfHeavenMonsterDrop(context, gameConfiguration, boxOfLuck);
        if (gameConfiguration.Monsters.FirstOrDefault(m => m.Number == CarenTheBarmaidNumber)?.MerchantStore is { } store)
        {
            await ApplyBoxesOfKundunAsync(context, store, boxOfLuck).ConfigureAwait(false);
        }

        MoveArchangelWeaponsToBoxOfKundunPlus5(gameConfiguration, boxOfLuck);
        LimitBoxOfKundunDrops(gameConfiguration, boxOfLuck);
    }

    /// <summary>
    /// Gets the excellent item group of a Box of Kundun.
    /// </summary>
    /// <param name="boxOfLuck">The Box of Luck.</param>
    /// <param name="level">The level of the Box of Luck, which is the Box of Kundun.</param>
    /// <returns>The excellent item group, if it exists.</returns>
    internal static ItemDropItemGroup? GetBoxOfKundunItemGroup(ItemDefinition boxOfLuck, byte level)
        => boxOfLuck.DropItems.FirstOrDefault(g => g.SourceItemLevel == level && g.ItemType == SpecialItemType.Excellent);

    /// <summary>
    /// Gets the Box of Luck, whose levels are the Box of Heaven and the Boxes of Kundun.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The Box of Luck, if it exists.</returns>
    internal static ItemDefinition? GetBoxOfLuck(GameConfiguration gameConfiguration)
        => gameConfiguration.Items.FirstOrDefault(i => i.Group == BoxOfLuckGroup && i.Number == BoxOfLuckNumber);

    /// <summary>
    /// Determines whether the item can drop from the Box of Heaven.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c>, if it can drop from the Box of Heaven.</returns>
    internal static bool IsBoxOfHeavenItem(ItemDefinition item)
        => item.Group <= LastEquipmentGroup && item.DropLevel <= BoxOfHeavenMaximumDropLevel && item.IsActive && item.DropsFromMonsters && !item.IsAmmunition && item.ItemSlot is not null;

    /// <summary>
    /// Gets the identifier of the drop group by which monsters drop the Box of Heaven.
    /// </summary>
    /// <returns>The identifier.</returns>
    internal static Guid GetBoxOfHeavenMonsterDropId()
        => GuidHelper.CreateGuid<DropItemGroup>(BoxOfLuckGroup, BoxOfLuckNumber, BoxOfHeavenLevel);

    private static void ApplyBoxOfHeavenMonsterDrop(IContext context, GameConfiguration gameConfiguration, ItemDefinition boxOfLuck)
    {
        var id = GetBoxOfHeavenMonsterDropId();
        var dropGroup = gameConfiguration.DropItemGroups.FirstOrDefault(g => g.GetId() == id);
        if (dropGroup is null)
        {
            dropGroup = context.CreateNew<DropItemGroup>();
            dropGroup.SetGuid(id);
            gameConfiguration.DropItemGroups.Add(dropGroup);
        }

        dropGroup.Description = "Box of Heaven (0.1% drop chance)";
        dropGroup.Chance = BoxOfHeavenDropChance;
        dropGroup.MinimumMonsterLevel = BoxOfHeavenMinimumMonsterLevel;
        dropGroup.MaximumMonsterLevel = null;
        dropGroup.ItemType = SpecialItemType.None;
        dropGroup.ItemLevel = BoxOfHeavenLevel;
        if (dropGroup.PossibleItems.Count != 1 || !dropGroup.PossibleItems.Contains(boxOfLuck))
        {
            dropGroup.PossibleItems.Clear();
            dropGroup.PossibleItems.Add(boxOfLuck);
        }

        var jewelsId = GuidHelper.CreateGuid<DropItemGroup>(JewelsDropGroupNumber);
        foreach (var map in gameConfiguration.Maps.Where(m => m.DropItemGroups.Any(g => g.GetId() == jewelsId)))
        {
            if (!map.DropItemGroups.Contains(dropGroup))
            {
                map.DropItemGroups.Add(dropGroup);
            }
        }
    }

    private static async ValueTask ApplyBoxOfHeavenAsync(IContext context, GameConfiguration gameConfiguration, ItemDefinition boxOfLuck)
    {
        var groups = boxOfLuck.DropItems.Where(g => g.SourceItemLevel == BoxOfHeavenLevel).ToList();
        var itemGroup = groups.FirstOrDefault(g => g.ItemType != SpecialItemType.Money);

        // The item always drops, so the zen fallback isn't needed anymore. With the same chance,
        // it could even be selected before the item group.
        foreach (var group in groups.Where(g => g != itemGroup))
        {
            boxOfLuck.DropItems.Remove(group);
            await context.DeleteAsync(group).ConfigureAwait(false);
        }

        if (itemGroup is null)
        {
            itemGroup = context.CreateNew<ItemDropItemGroup>();
            itemGroup.SourceItemLevel = BoxOfHeavenLevel;
            itemGroup.DropEffect = ItemDropEffect.FanfareSound;
            itemGroup.Description = LocalizedString.FromResource(() => ItemDropDescriptions.BoxOfHeaven);
            boxOfLuck.DropItems.Add(itemGroup);
        }

        itemGroup.ItemType = SpecialItemType.RandomItem;
        itemGroup.Chance = 1.0;
        itemGroup.MinimumLevel = BoxOfHeavenMinimumLevel;
        itemGroup.MaximumLevel = BoxOfHeavenMaximumLevel;
        itemGroup.ExcellentOptionsAddChance = null;
        itemGroup.ExcellentOptionsMaximumOptionsPerItem = null;

        var items = gameConfiguration.Items.Where(IsBoxOfHeavenItem).ToHashSet();
        foreach (var item in itemGroup.PossibleItems.Where(i => !items.Contains(i)).ToList())
        {
            itemGroup.PossibleItems.Remove(item);
        }

        foreach (var item in items.Where(i => !itemGroup.PossibleItems.Contains(i)).OrderBy(i => i.Group).ThenBy(i => i.Number))
        {
            itemGroup.PossibleItems.Add(item);
        }
    }

    private static void MoveArchangelWeaponsToBoxOfKundunPlus5(GameConfiguration gameConfiguration, ItemDefinition boxOfLuck)
    {
        if (GetBoxOfKundunItemGroup(boxOfLuck, LastBoxOfKundunLevel) is not { } plus5)
        {
            return;
        }

        var plus4 = GetBoxOfKundunItemGroup(boxOfLuck, BoxOfKundunPlus4Level);
        var weapons = gameConfiguration.Items.Where(i => ArchangelWeapons.Contains((i.Group, i.Number)));
        foreach (var weapon in weapons)
        {
            plus4?.PossibleItems.Remove(weapon);
            if (!plus5.PossibleItems.Contains(weapon))
            {
                plus5.PossibleItems.Add(weapon);
            }
        }
    }

    /// <summary>
    /// Lets the monsters which drop a Box of Kundun above the <see cref="HighestBoxOfKundunLevel"/> drop the highest one instead.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="boxOfLuck">The Box of Luck.</param>
    private static void LimitBoxOfKundunDrops(GameConfiguration gameConfiguration, ItemDefinition boxOfLuck)
    {
        // The levels above the Box of Kundun +5 are other items, e.g. the Heart of Dark Lord (+13).
        foreach (var dropGroup in gameConfiguration.DropItemGroups.Where(g => g.ItemLevel is > HighestBoxOfKundunLevel and <= LastBoxOfKundunLevel && g.PossibleItems.Contains(boxOfLuck)))
        {
            dropGroup.ItemLevel = HighestBoxOfKundunLevel;
            dropGroup.Description = LocalizedString.FromResource(() => DropGroupDescriptions.BoxOfKundunPlus4);
        }
    }

    private static async ValueTask ApplyBoxesOfKundunAsync(IContext context, ItemStorage store, ItemDefinition boxOfLuck)
    {
        foreach (var item in store.Items.Where(i => i.Definition == boxOfLuck && i.Level > HighestBoxOfKundunLevel).ToList())
        {
            store.Items.Remove(item);
            await context.DeleteAsync(item).ConfigureAwait(false);
        }

        for (var level = FirstBoxOfKundunLevel; level <= HighestBoxOfKundunLevel; level++)
        {
            if (store.Items.Any(i => i.Definition == boxOfLuck && i.Level == level)
                || ClassicMerchantStores.FindFreeSlot(store.Items, boxOfLuck) is not { } slot)
            {
                continue;
            }

            var item = context.CreateNew<Item>();
            item.Definition = boxOfLuck;
            item.Level = level;
            item.Durability = 1;
            item.ItemSlot = slot;
            store.Items.Add(item);
        }
    }
}
