// <copyright file="MerchantStoreExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Extension methods for the <see cref="ItemStorage"/> of a merchant store.
/// </summary>
public static class MerchantStoreExtensions
{
    /// <summary>
    /// Gets the items of a merchant store which can be offered to the player.
    /// Items whose <see cref="ItemDefinition.IsActive"/> is <c>false</c> are left out.
    /// </summary>
    /// <param name="merchantStore">The merchant store.</param>
    /// <returns>The items which can be offered to the player.</returns>
    public static ICollection<Item> GetOfferedItems(this ItemStorage merchantStore)
    {
        if (merchantStore.Items.All(item => item.Definition?.IsActive is not false))
        {
            return merchantStore.Items;
        }

        return merchantStore.Items.Where(item => item.Definition?.IsActive is not false).ToList();
    }

    /// <summary>
    /// Gets the items which a merchant NPC offers to the player. They're provided by an <see cref="IMerchantStoreItemsPlugIn"/>,
    /// or are the items of the configured merchant store.
    /// Items whose <see cref="ItemDefinition.IsActive"/> is <c>false</c> are left out.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="merchant">The merchant NPC.</param>
    /// <returns>The items which can be offered to the player, or <c>null</c> if the NPC isn't a merchant.</returns>
    public static ICollection<Item>? GetOfferedItems(this Player player, NonPlayerCharacter merchant)
    {
        if (player.GameContext.PlugInManager.GetPlugInPoint<IMerchantStoreItemsPlugIn>() is { } plugIn)
        {
            var eventArgs = new MerchantStoreItemsArgs();
            plugIn.ProvideStoreItems(player, merchant, eventArgs);
            if (eventArgs.Items is { } items)
            {
                return items.Where(item => item.Definition?.IsActive is not false).ToList();
            }
        }

        return merchant.Definition.MerchantStore?.GetOfferedItems();
    }
}
