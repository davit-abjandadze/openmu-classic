// <copyright file="MedalItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// The medal items (Box of Luck +5 and +6), which drop e.g. from bosses.
/// They are added to the <see cref="MedalStats"/> of a character, so that they can be exchanged with /medals.
/// </summary>
public static class MedalItems
{
    /// <summary>
    /// The group of the Box of Luck, whose levels are the medals.
    /// </summary>
    public const byte Group = 14;

    /// <summary>
    /// The number of the Box of Luck, whose levels are the medals.
    /// </summary>
    public const short Number = 11;

    /// <summary>
    /// The level of the Box of Luck which is the Silver Medal.
    /// </summary>
    public const byte SilverMedalLevel = 5;

    /// <summary>
    /// The level of the Box of Luck which is the Gold Medal.
    /// </summary>
    public const byte GoldMedalLevel = 6;

    /// <summary>
    /// Removes the medal items from the inventory of the player and adds them to the medals of the character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The number of deposited gold and silver medals.</returns>
    public static async ValueTask<(int Gold, int Silver)> DepositAsync(Player player)
    {
        if (player.Inventory is not { } inventory || player.Attributes is not { } attributes)
        {
            return (0, 0);
        }

        var medals = inventory.Items.Where(IsMedal).ToList();
        if (medals.Count == 0)
        {
            return (0, 0);
        }

        var gold = medals.Count(m => m.Level == GoldMedalLevel);
        var silver = medals.Count - gold;
        await RemoveAsync(player, medals, () =>
        {
            attributes[MedalStats.GoldMedals] += gold;
            attributes[MedalStats.SilverMedals] += silver;
        }).ConfigureAwait(false);

        return (gold, silver);
    }

    /// <summary>
    /// Counts the silver medal items in the inventory of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The number of silver medal items.</returns>
    public static int CountSilver(Player player)
        => player.Inventory?.Items.Count(IsSilverMedal) ?? 0;

    /// <summary>
    /// Removes silver medal items from the inventory of the player, without adding them to the medals of the character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="count">The number of silver medal items to remove.</param>
    /// <param name="onRemoved">An action which is executed within the persistence lock, after the items were removed.</param>
    /// <returns><c>true</c>, if the player had enough silver medal items and they were removed.</returns>
    public static async ValueTask<bool> TryRemoveSilverAsync(Player player, int count, Action onRemoved)
    {
        var medals = player.Inventory?.Items.Where(IsSilverMedal).Take(count).ToList() ?? [];
        if (medals.Count < count)
        {
            return false;
        }

        await RemoveAsync(player, medals, onRemoved).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Determines whether the item is a medal item in the inventory, not equipped and not in the personal store.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c>, if it's a medal.</returns>
    internal static bool IsMedal(Item item)
        => item.Definition is { Group: Group, Number: Number }
           && item.Level is SilverMedalLevel or GoldMedalLevel
           && item.ItemSlot > InventoryConstants.LastEquippableItemSlotIndex
           && item.ItemSlot < InventoryConstants.FirstStoreItemSlotIndex;

    /// <summary>
    /// Determines whether the item is a silver medal item in the inventory, not equipped and not in the personal store.
    /// Silver medals can't be dropped to the floor.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c>, if it's a silver medal.</returns>
    internal static bool IsSilverMedal(Item item) => IsMedal(item) && item.Level == SilverMedalLevel;

    private static async ValueTask RemoveAsync(Player player, IReadOnlyList<Item> medals, Action onRemoved)
    {
        await player.RunPersistenceExclusiveAsync(async () =>
        {
            foreach (var medal in medals)
            {
                await player.Inventory!.RemoveItemAsync(medal).ConfigureAwait(false);
                await player.PersistenceContext.DeleteAsync(medal).ConfigureAwait(false);
            }

            onRemoved();
        }).ConfigureAwait(false);

        foreach (var medal in medals)
        {
            await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(medal.ItemSlot)).ConfigureAwait(false);
        }
    }
}
