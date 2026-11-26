// <copyright file="CastleSiegeRewardDelivery.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.CastleSiege;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Persistence;

/// <summary>
/// Creates participant reward items and queues rewards which cannot be delivered immediately.
/// </summary>
internal static class CastleSiegeRewardDelivery
{
    /// <summary>
    /// Tries to add a configured reward item to a player's inventory.
    /// </summary>
    /// <param name="player">The rewarded player.</param>
    /// <param name="rewardDefinition">The reward item definition.</param>
    /// <param name="itemLevel">The item level of the reward item.</param>
    /// <returns><c>true</c> when the item was added; otherwise, <c>false</c>.</returns>
    internal static async ValueTask<bool> TryAddToInventoryAsync(Player player, ItemDefinition rewardDefinition, byte itemLevel)
    {
        if (player.Inventory is null)
        {
            return false;
        }

        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = rewardDefinition;
        item.Level = Math.Min(itemLevel, rewardDefinition.MaximumItemLevel);
        item.Durability = item.IsStackable() ? 1 : rewardDefinition.Durability;
        if (await player.Inventory.AddItemAsync(item).ConfigureAwait(false))
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(
                    plugIn => plugIn.ItemAppearAsync(item))
                .ConfigureAwait(false);
            return true;
        }

        await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
        return false;
    }

    /// <summary>
    /// Queues rewards for delivery when the characters next enter the game.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="characterRewards">The persistent character identifiers with the number of items which are queued for them.</param>
    /// <param name="rewardDefinition">The reward item definition.</param>
    /// <param name="itemLevel">The item level of the reward items.</param>
    /// <returns>A task that represents the asynchronous queue operation.</returns>
    internal static async ValueTask QueueAsync(
        IGameContext gameContext,
        IReadOnlyCollection<(Guid CharacterId, int Count)> characterRewards,
        ItemDefinition rewardDefinition,
        byte itemLevel)
    {
        if (characterRewards.Count == 0)
        {
            return;
        }

        using var persistenceContext = gameContext.PersistenceContextProvider.CreateNewTypedContext(
            typeof(CastleSiegePendingReward),
            false,
            gameContext.Configuration);
        foreach (var (characterId, count) in characterRewards)
        {
            for (var i = 0; i < count; i++)
            {
                var pendingReward = persistenceContext.CreateNew<CastleSiegePendingReward>();
                pendingReward.CharacterId = characterId;
                pendingReward.ItemDefinitionId = rewardDefinition.GetId();
                pendingReward.ItemLevel = itemLevel;
            }
        }

        await persistenceContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
