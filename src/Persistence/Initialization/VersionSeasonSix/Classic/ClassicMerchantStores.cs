// <copyright file="ClassicMerchantStores.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>
/// Adapts the merchant stores of the classic server:
/// <list type="bullet">
///   <item>The potion stacks of three pieces are removed from all stores.</item>
///   <item>All stores sell stacks of <see cref="LargePotionStackSize"/> large healing and large mana potions.</item>
///   <item>Every active skill scroll and orb is sold by the store of its class:
///     Dark Wizard at Pasi the Mage (Lorencia), Dark Knight at Hanzo the Blacksmith (Lorencia), Fairy Elf at Elf Lala (Noria).
///     Scrolls and orbs of other classes are removed from these stores.
///     Pasi the Mage doesn't sell the Pad set anymore, because the scrolls wouldn't fit into the store otherwise.</item>
/// </list>
/// </summary>
/// <remarks>
/// The stack size is the durability of the item, which is a single byte in the item data of the client protocol,
/// so <see cref="LargePotionStackSize"/> is the biggest possible stack.
/// The item definitions keep their stack size of three, so dropped potions and the stacking in the inventory stay as they are.
/// </remarks>
public static class ClassicMerchantStores
{
    /// <summary>
    /// The number of potions in a large potion stack which the stores sell. It's the maximum of the client protocol.
    /// </summary>
    public const byte LargePotionStackSize = byte.MaxValue;

    /// <summary>
    /// The number of pieces of the potion stacks which are removed from the stores.
    /// </summary>
    public const byte RemovedStackSize = 3;

    private const byte PotionGroup = 14;

    private const short LargeHealingPotionNumber = 3;

    private const short LargeManaPotionNumber = 6;

    private const byte OrbsGroup = 12;

    private const byte ScrollsGroup = 15;

    private const short HanzoTheBlacksmithNumber = 251;

    private const short PasiTheMageNumber = 254;

    private const short ElfLalaNumber = 242;

    /// <summary>
    /// The items (group, number) of the Pad set, which Pasi the Mage doesn't sell anymore to make room for the scrolls.
    /// </summary>
    private static readonly IReadOnlySet<(byte Group, short Number)> PasiRemovedItems = new HashSet<(byte Group, short Number)>
    {
        (7, 2), // Pad Helm
        (8, 2), // Pad Armor
        (9, 2), // Pad Pants
        (10, 2), // Pad Gloves
        (11, 2), // Pad Boots
    };

    private static readonly int StoreSlots = InventoryConstants.RowSize * InventoryConstants.WarehouseRows;

    /// <summary>
    /// The class families whose skill scrolls and orbs are sold by a merchant, by the number of the merchant.
    /// </summary>
    private static readonly IReadOnlyDictionary<short, IReadOnlySet<byte>> SkillStores = new Dictionary<short, IReadOnlySet<byte>>
    {
        [PasiTheMageNumber] = new HashSet<byte>
        {
            (byte)CharacterClassNumber.DarkWizard,
            (byte)CharacterClassNumber.SoulMaster,
            (byte)CharacterClassNumber.GrandMaster,
        },
        [HanzoTheBlacksmithNumber] = new HashSet<byte>
        {
            (byte)CharacterClassNumber.DarkKnight,
            (byte)CharacterClassNumber.BladeKnight,
            (byte)CharacterClassNumber.BladeMaster,
        },
        [ElfLalaNumber] = new HashSet<byte>
        {
            (byte)CharacterClassNumber.FairyElf,
            (byte)CharacterClassNumber.MuseElf,
            (byte)CharacterClassNumber.HighElf,
        },
    };

    /// <summary>
    /// Applies the store changes to all merchant stores of the game configuration.
    /// It can be applied multiple times; a store which already sells an item doesn't get it again.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns>The task.</returns>
    public static async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        var largePotions = gameConfiguration.Items
            .Where(i => i.Group == PotionGroup && i.Number is LargeHealingPotionNumber or LargeManaPotionNumber)
            .OrderBy(i => i.Number)
            .ToList();

        foreach (var store in gameConfiguration.Monsters.Select(m => m.MerchantStore).OfType<ItemStorage>())
        {
            await RemoveThreePieceStacksAsync(context, store).ConfigureAwait(false);
            foreach (var potion in largePotions)
            {
                AddLargeStack(context, store, potion);
            }
        }

        foreach (var (merchantNumber, classes) in SkillStores)
        {
            if (gameConfiguration.Monsters.FirstOrDefault(m => m.Number == merchantNumber)?.MerchantStore is { } store)
            {
                await ApplySkillStoreAsync(context, gameConfiguration, store, merchantNumber, classes).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask ApplySkillStoreAsync(IContext context, GameConfiguration gameConfiguration, ItemStorage store, short merchantNumber, IReadOnlySet<byte> classes)
    {
        bool IsForClasses(ItemDefinition? definition)
            => definition is not null && definition.QualifiedCharacters.Any(c => classes.Contains(c.Number));

        var removedItems = store.Items
            .Where(i => (IsSkillLearningItem(i.Definition) && !IsForClasses(i.Definition))
                        || (merchantNumber == PasiTheMageNumber && i.Definition is { } definition && PasiRemovedItems.Contains((definition.Group, definition.Number))))
            .ToList();
        foreach (var item in removedItems)
        {
            store.Items.Remove(item);
            await context.DeleteAsync(item).ConfigureAwait(false);
        }

        var missingSkillItems = gameConfiguration.Items
            .Where(i => i.IsActive && IsSkillLearningItem(i) && IsForClasses(i))
            .OrderByDescending(i => i.Group)
            .ThenBy(i => i.Number)

            // The level of the orb of summoning defines the summoned monster, so each level is a different skill.
            .SelectMany(i => Enumerable.Range(0, i.MaximumItemLevel + 1).Select(level => (Definition: i, Level: (byte)level)))
            .Where(skill => !store.Items.Any(i => i.Definition == skill.Definition && i.Level == skill.Level))
            .ToList();
        if (missingSkillItems.Count == 0)
        {
            return;
        }

        // The stores are quite full, so the items are packed again from the top left, in their previous order.
        var placed = new List<Item>();
        foreach (var item in store.Items.Where(i => i.Definition is not null).OrderBy(i => i.ItemSlot).ToList())
        {
            if (FindFreeSlot(placed, item.Definition!) is { } slot)
            {
                item.ItemSlot = slot;
            }

            placed.Add(item);
        }

        foreach (var (definition, level) in missingSkillItems)
        {
            if (FindFreeSlot(placed, definition) is not { } slot)
            {
                continue;
            }

            var item = context.CreateNew<Item>();
            item.Definition = definition;
            item.Level = level;
            item.Durability = 1;
            item.ItemSlot = slot;
            store.Items.Add(item);
            placed.Add(item);
        }
    }

    private static bool IsSkillLearningItem(ItemDefinition? definition)
        => definition?.Skill is not null && definition.Group is OrbsGroup or ScrollsGroup;

    private static async ValueTask RemoveThreePieceStacksAsync(IContext context, ItemStorage store)
    {
        var stacks = store.Items
            .Where(i => i.Definition?.Group == PotionGroup && (int)i.Durability == RemovedStackSize)
            .ToList();
        foreach (var stack in stacks)
        {
            store.Items.Remove(stack);
            await context.DeleteAsync(stack).ConfigureAwait(false);
        }
    }

    private static void AddLargeStack(IContext context, ItemStorage store, ItemDefinition potion)
    {
        if (store.Items.Any(i => i.Definition == potion && (int)i.Durability == LargePotionStackSize))
        {
            return;
        }

        if (FindFreeSlot(store.Items, potion) is not { } slot)
        {
            return;
        }

        var item = context.CreateNew<Item>();
        item.Definition = potion;
        item.Durability = LargePotionStackSize;
        item.ItemSlot = slot;
        store.Items.Add(item);
    }

    /// <summary>
    /// Finds the first free slot of a store for the item, from the top left.
    /// </summary>
    /// <param name="occupyingItems">The items which occupy the store.</param>
    /// <param name="definition">The definition of the item which should be placed.</param>
    /// <returns>The free slot, if there is one.</returns>
    internal static byte? FindFreeSlot(IEnumerable<Item> occupyingItems, ItemDefinition definition)
    {
        var rows = StoreSlots / InventoryConstants.RowSize;
        var used = new bool[rows, InventoryConstants.RowSize];
        foreach (var item in occupyingItems)
        {
            var row = item.ItemSlot / InventoryConstants.RowSize;
            var column = item.ItemSlot % InventoryConstants.RowSize;
            for (var r = row; r < Math.Min(rows, row + (item.Definition?.Height ?? 1)); r++)
            {
                for (var c = column; c < Math.Min(InventoryConstants.RowSize, column + (item.Definition?.Width ?? 1)); c++)
                {
                    used[r, c] = true;
                }
            }
        }

        for (var row = 0; row <= rows - definition.Height; row++)
        {
            for (var column = 0; column <= InventoryConstants.RowSize - definition.Width; column++)
            {
                if (IsFree(used, row, column, definition))
                {
                    return (byte)((row * InventoryConstants.RowSize) + column);
                }
            }
        }

        return null;
    }

    private static bool IsFree(bool[,] used, int row, int column, ItemDefinition definition)
    {
        for (var r = row; r < row + definition.Height; r++)
        {
            for (var c = column; c < column + definition.Width; c++)
            {
                if (used[r, c])
                {
                    return false;
                }
            }
        }

        return true;
    }
}
