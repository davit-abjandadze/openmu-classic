// <copyright file="ClassicJewelryDrops.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// Makes the rings and pendants rarer than the other equipment of the classic server:
/// <list type="bullet">
///   <item>They are not part of the random item and excellent item drops anymore, where each of them was
///     as likely as any weapon or armor piece of its drop level.</item>
///   <item>Instead, monsters drop a normal ring or pendant with a chance of <see cref="JewelryDropChance"/>,
///     out of the ones whose drop level isn't above the monster level.</item>
///   <item>Monsters of at least level <see cref="ExcellentJewelryMinimumMonsterLevel"/> drop an excellent ring or pendant
///     with a chance of <see cref="ExcellentJewelryDropChance"/>.</item>
///   <item>Both drop on all maps which have the common jewel drop.</item>
/// </list>
/// The main source of the excellent rings and pendants stays the Box of Kundun: the +2 drops the ones of Ice, Poison,
/// Lighting and Fire, the +3 the ones of Fire, Earth, Wind, Magic, Ice, Wind, Water and Ability.
/// </summary>
public static class ClassicJewelryDrops
{
    /// <summary>
    /// The chance that a monster drops a normal ring or pendant (0.5 %).
    /// </summary>
    public const double JewelryDropChance = 0.005;

    /// <summary>
    /// The chance that a monster drops an excellent ring or pendant (0.02 %).
    /// </summary>
    /// <remarks>
    /// That's 2 % of the common excellent drop chance (see <see cref="ClassicRates.ExcellentDropChance"/>),
    /// so that the Box of Kundun stays the main source of them.
    /// </remarks>
    public const double ExcellentJewelryDropChance = 0.0002;

    /// <summary>
    /// The minimum level of the monsters which drop excellent rings and pendants. Like the common excellent drop,
    /// which selects items of 25 levels below the monster level, it keeps them out of the beginner areas.
    /// </summary>
    public const byte ExcellentJewelryMinimumMonsterLevel = 50;

    /// <summary>
    /// The rings and pendants (group, number) of the classic server, which drop with their own chance.
    /// </summary>
    public static readonly IReadOnlyList<(byte Group, short Number)> Jewelry =
    [
        (13, 8), // Ring of Ice
        (13, 9), // Ring of Poison
        (13, 12), // Pendant of Lighting
        (13, 13), // Pendant of Fire
        (13, 21), // Ring of Fire
        (13, 22), // Ring of Earth
        (13, 23), // Ring of Wind
        (13, 24), // Ring of Magic
        (13, 25), // Pendant of Ice
        (13, 26), // Pendant of Wind
        (13, 27), // Pendant of Water
        (13, 28), // Pendant of Ability
    ];

    /// <summary>
    /// The first number of the identifiers of the jewelry drop groups: the item group of the rings and pendants,
    /// and an item number which doesn't exist, so that they don't collide with other drop groups.
    /// </summary>
    private const short DropGroupIdPrefix = 13;

    private const short DropGroupIdNumber = 255;

    /// <summary>
    /// The number of the common jewel drop group (see <see cref="GameConfigurationInitializerBase"/>),
    /// whose maps also get the jewelry drops.
    /// </summary>
    private const short JewelsDropGroupNumber = 4;

    /// <summary>
    /// Applies the jewelry drops. It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        var items = gameConfiguration.Items.Where(i => Jewelry.Contains((i.Group, i.Number))).OrderBy(i => i.Number).ToList();
        foreach (var item in items)
        {
            item.DropsFromMonsters = false;
        }

        var normal = GetOrCreateDropGroup(context, gameConfiguration, GetNormalDropGroupId());
        normal.Description = "Rings and pendants (0.5% drop chance)";
        normal.Chance = JewelryDropChance;
        normal.ItemType = SpecialItemType.None;
        normal.MinimumMonsterLevel = null;
        SetItems(normal, items);

        var excellent = GetOrCreateDropGroup(context, gameConfiguration, GetExcellentDropGroupId());
        excellent.Description = "Excellent rings and pendants (0.02% drop chance, monster level 50+)";
        excellent.Chance = ExcellentJewelryDropChance;
        excellent.ItemType = SpecialItemType.Excellent;
        excellent.MinimumMonsterLevel = ExcellentJewelryMinimumMonsterLevel;
        SetItems(excellent, items);

        var jewelsId = GuidHelper.CreateGuid<DropItemGroup>(JewelsDropGroupNumber);
        foreach (var map in gameConfiguration.Maps.Where(m => m.DropItemGroups.Any(g => g.GetId() == jewelsId)))
        {
            AddOnce(map, normal);
            AddOnce(map, excellent);
        }
    }

    /// <summary>
    /// Gets the identifier of the drop group of the normal rings and pendants.
    /// </summary>
    /// <returns>The identifier.</returns>
    internal static Guid GetNormalDropGroupId()
        => GuidHelper.CreateGuid<DropItemGroup>(DropGroupIdPrefix, DropGroupIdNumber, 0);

    /// <summary>
    /// Gets the identifier of the drop group of the excellent rings and pendants.
    /// </summary>
    /// <returns>The identifier.</returns>
    internal static Guid GetExcellentDropGroupId()
        => GuidHelper.CreateGuid<DropItemGroup>(DropGroupIdPrefix, DropGroupIdNumber, 1);

    private static DropItemGroup GetOrCreateDropGroup(IContext context, GameConfiguration gameConfiguration, Guid id)
    {
        if (gameConfiguration.DropItemGroups.FirstOrDefault(g => g.GetId() == id) is { } existing)
        {
            return existing;
        }

        var dropGroup = context.CreateNew<DropItemGroup>();
        dropGroup.SetGuid(id);
        dropGroup.MaximumMonsterLevel = null;
        dropGroup.ItemLevel = null;
        gameConfiguration.DropItemGroups.Add(dropGroup);
        return dropGroup;
    }

    private static void SetItems(DropItemGroup dropGroup, IReadOnlyCollection<ItemDefinition> items)
    {
        foreach (var item in dropGroup.PossibleItems.Where(i => !items.Contains(i)).ToList())
        {
            dropGroup.PossibleItems.Remove(item);
        }

        foreach (var item in items.Where(i => !dropGroup.PossibleItems.Contains(i)))
        {
            dropGroup.PossibleItems.Add(item);
        }
    }

    private static void AddOnce(GameMapDefinition map, DropItemGroup dropGroup)
    {
        if (!map.DropItemGroups.Contains(dropGroup))
        {
            map.DropItemGroups.Add(dropGroup);
        }
    }
}
