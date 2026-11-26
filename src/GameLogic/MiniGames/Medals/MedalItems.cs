// <copyright file="MedalItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// The silver medal item (Box of Luck +5), which drops e.g. from bosses and is given for the results of the Devil Square and the Blood Castle.
/// It pays at the <see cref="MedalMerchantPlugIn"/>, like the silver medals of the <see cref="MedalStats"/> of a character.
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
    /// Awards silver medals to the player, e.g. for winning an event, and tells the player about it.
    /// The silver medals are put into the inventory as items; the ones which don't fit are added to the
    /// <see cref="MedalStats.SilverMedals"/> of the character, which pay at the merchant as well.
    /// The progress of the player isn't saved.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="silver">The number of silver medals.</param>
    /// <returns>The task.</returns>
    public static async ValueTask AwardSilverAsync(Player player, int silver)
    {
        if (silver <= 0 || player.Attributes is not { } attributes)
        {
            return;
        }

        var credited = silver - await TryGiveSilverAsync(player, silver).ConfigureAwait(false);
        attributes[MedalStats.SilverMedals] += credited;

        var total = (int)attributes[MedalStats.SilverMedals] + CountSilver(player);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsAwarded), silver, total).ConfigureAwait(false);
        if (credited > 0)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalsCreditedInventoryFull), credited).ConfigureAwait(false);
        }
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

        return true;
    }

    /// <summary>
    /// Determines whether the item is a silver medal item in the inventory, not equipped and not in the personal store.
    /// Silver medals can't be dropped to the floor.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns><c>true</c>, if it's a silver medal.</returns>
    internal static bool IsSilverMedal(Item item)
        => item.Definition is { Group: Group, Number: Number }
           && item.Level == SilverMedalLevel
           && item.ItemSlot > InventoryConstants.LastEquippableItemSlotIndex
           && item.ItemSlot < InventoryConstants.FirstStoreItemSlotIndex;

    /// <summary>
    /// Puts silver medal items into the inventory of the player, as long as it has free space.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="count">The number of silver medal items to give.</param>
    /// <returns>The number of silver medal items which were put into the inventory.</returns>
    private static async ValueTask<int> TryGiveSilverAsync(Player player, int count)
    {
        if (RewardItems.FindDefinition(player, Group, Number) is not { } definition)
        {
            return 0;
        }

        var given = 0;
        while (given < count && await RewardItems.TryGiveAsync(player, definition, SilverMedalLevel).ConfigureAwait(false) is not null)
        {
            given++;
        }

        return given;
    }
}
