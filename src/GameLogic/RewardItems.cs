// <copyright file="RewardItems.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Helper to give reward items directly into the inventory of a player.
/// </summary>
public static class RewardItems
{
    /// <summary>
    /// Finds the definition of an item.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="group">The group of the item.</param>
    /// <param name="number">The number of the item.</param>
    /// <returns>The item definition, or <c>null</c>.</returns>
    public static ItemDefinition? FindDefinition(Player player, byte group, short number)
        => player.GameContext.Configuration.Items.FirstOrDefault(i => i.Group == group && i.Number == number);

    /// <summary>
    /// Gets the name of a reward, which is either the configured name or the name of the item with its level.
    /// </summary>
    /// <param name="player">The player, whose language is used.</param>
    /// <param name="configuredName">The configured name.</param>
    /// <param name="group">The group of the item.</param>
    /// <param name="number">The number of the item.</param>
    /// <param name="level">The level of the item.</param>
    /// <returns>The name.</returns>
    public static string GetName(Player player, string? configuredName, byte group, short number, byte level)
    {
        if (!string.IsNullOrWhiteSpace(configuredName))
        {
            return configuredName;
        }

        var name = FindDefinition(player, group, number)?.Name.GetTranslation(player.Culture) ?? "?";
        return level > 0 ? $"{name} +{level}" : name;
    }

    /// <summary>
    /// Tries to create an item and to add it to the inventory of the player.
    /// The item is shown to the player, if it was added.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="definition">The definition of the item.</param>
    /// <param name="level">The level of the item.</param>
    /// <param name="onAdded">An action which is executed within the persistence lock, after the item was added, e.g. to pay for it.</param>
    /// <param name="configure">An optional action to configure the item before it's added, e.g. to add options.</param>
    /// <returns>The added item, or <c>null</c> if the inventory is full.</returns>
    public static async ValueTask<Item?> TryGiveAsync(Player player, ItemDefinition definition, byte level, Action? onAdded = null, Action<Item>? configure = null)
    {
        var item = await player.RunPersistenceExclusiveAsync(async () =>
        {
            if (player.Inventory is not { } inventory)
            {
                return null;
            }

            var newItem = player.PersistenceContext.CreateNew<Item>();
            newItem.Definition = definition;
            newItem.Level = level;
            newItem.Durability = definition.Durability;
            configure?.Invoke(newItem);
            if (await inventory.AddItemAsync(newItem).ConfigureAwait(false))
            {
                onAdded?.Invoke();
                return newItem;
            }

            await player.PersistenceContext.DeleteAsync(newItem).ConfigureAwait(false);
            return (Item?)null;
        }).ConfigureAwait(false);

        if (item is not null)
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(item)).ConfigureAwait(false);
        }

        return item;
    }
}
