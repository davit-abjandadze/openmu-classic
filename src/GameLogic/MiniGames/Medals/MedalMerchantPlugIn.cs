// <copyright file="MedalMerchantPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Administration;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A merchant NPC which sells items for silver medals instead of zen, by default
/// Thompson the Merchant with the Boxes of Kundun +4 and +5.
/// The merchant offers the items of the configuration instead of the items of its merchant store,
/// so items can be added, changed and removed in the configuration while the server is running.
/// The silver medal items in the inventory, e.g. dropped by bosses, can pay as well; they're only removed when the purchase succeeds.
/// </summary>
[Guid("3E9D4B27-6C1A-4D85-B0F3-8A2E5C7D1B12")]
[PlugIn]
[Display(Name = "Medal merchant", Description = "A merchant NPC which sells the configured items for silver medals instead of zen, by default Thompson the Merchant with the Boxes of Kundun. Items can be added to the configuration while the server is running.")]
public class MedalMerchantPlugIn : INpcItemPurchasePlugIn, IMerchantStoreItemsPlugIn, ISupportCustomConfiguration<MedalMerchantConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private static readonly int StoreColumns = InventoryConstants.RowSize;

    private static readonly int StoreRows = InventoryConstants.WarehouseRows;

    private Store? _store;

    /// <inheritdoc />
    public MedalMerchantConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => MedalMerchantConfiguration.CreateDefault();

    /// <inheritdoc />
    public void ProvideStoreItems(Player player, NonPlayerCharacter merchant, MerchantStoreItemsArgs eventArgs)
    {
        if (this.GetStore(player, merchant) is { } store)
        {
            eventArgs.Items = store.Items;
        }
    }

    /// <inheritdoc />
    public async ValueTask BeforePurchaseAsync(Player player, NonPlayerCharacter merchant, NpcItemPurchaseArgs eventArgs)
    {
        if (this.Configuration is not { } configuration
            || merchant.Definition.Number != configuration.MerchantNumber)
        {
            return;
        }

        if (this.GetPrice(configuration, eventArgs.StoreItem) is not { } price || player.Attributes is not { } attributes)
        {
            eventArgs.Cancel = true;
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemUnknown)).ConfigureAwait(false);
            return;
        }

        // The medals are paid after the item was added to the inventory, see AfterPurchaseAsync.
        // Until then, the medal items stay in the inventory.
        var silverMedals = GetAvailableSilverMedals(player, attributes);
        if (silverMedals < price.SilverMedals)
        {
            eventArgs.Cancel = true;
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalMerchantNotEnough), GetName(player, price), price.SilverMedals, silverMedals).ConfigureAwait(false);
            return;
        }

        eventArgs.Price = 0;
    }

    /// <inheritdoc />
    public async ValueTask AfterPurchaseAsync(Player player, NonPlayerCharacter merchant, NpcItemPurchaseArgs eventArgs, Item boughtItem)
    {
        if (this.Configuration is not { } configuration
            || merchant.Definition.Number != configuration.MerchantNumber
            || this.GetPrice(configuration, eventArgs.StoreItem) is not { } price
            || player.Attributes is not { } attributes)
        {
            return;
        }

        // The deposited medals are paid first, then the medal items of the inventory.
        var fromDeposit = Math.Min((int)attributes[MedalStats.SilverMedals], price.SilverMedals);
        await MedalItems.TryRemoveSilverAsync(player, price.SilverMedals - fromDeposit, () => attributes[MedalStats.SilverMedals] -= fromDeposit).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.MedalMerchantBought), GetName(player, price), price.SilverMedals, GetAvailableSilverMedals(player, attributes)).ConfigureAwait(false);
        await player.SaveProgressAsync().ConfigureAwait(false);
    }

    private static int GetAvailableSilverMedals(Player player, IAttributeSystem attributes)
        => (int)attributes[MedalStats.SilverMedals] + MedalItems.CountSilver(player);

    private static string GetName(Player player, MedalMerchantConfiguration.MedalPrice price)
        => RewardItems.GetName(player, price.Name, price.ItemGroup, price.ItemNumber, price.ItemLevel);

    /// <summary>
    /// Creates the items of the store of the configuration. An item is put into the first free slot
    /// where it fits, row by row. Items which don't fit or whose options aren't allowed by their definition are left out.
    /// </summary>
    private static Store CreateStore(MedalMerchantConfiguration configuration, GameConfiguration gameConfiguration)
    {
        var occupied = new bool[StoreRows, StoreColumns];
        var items = new List<Item>();
        var prices = new Dictionary<Item, MedalMerchantConfiguration.MedalPrice>(ReferenceEqualityComparer.Instance);
        foreach (var price in configuration.Prices)
        {
            var specification = new ItemSpecification(price.ItemGroup, price.ItemNumber, price.ItemLevel, price.Skill, price.Luck, price.OptionLevel, price.ExcellentOptions);
            if (gameConfiguration.Items.FirstOrDefault(i => i.Group == price.ItemGroup && i.Number == price.ItemNumber) is not { } definition
                || !GameMasterItemFactory.IsValid(definition, specification)
                || FindFreeSlot(occupied, definition.Width, definition.Height) is not { } slot)
            {
                continue;
            }

            var item = GameMasterItemFactory.CreateItem(definition, specification);
            if (item.IsWearable())
            {
                item.Durability = item.GetMaximumDurabilityOfOnePiece();
            }

            item.ItemSlot = slot;
            items.Add(item);
            prices.Add(item, price);
        }

        return new Store(configuration, gameConfiguration, items, prices);
    }

    private static byte? FindFreeSlot(bool[,] occupied, byte width, byte height)
    {
        width = Math.Max((byte)1, width);
        height = Math.Max((byte)1, height);
        for (var row = 0; row <= StoreRows - height; row++)
        {
            for (var column = 0; column <= StoreColumns - width; column++)
            {
                if (!IsFree(occupied, row, column, width, height))
                {
                    continue;
                }

                for (var r = row; r < row + height; r++)
                {
                    for (var c = column; c < column + width; c++)
                    {
                        occupied[r, c] = true;
                    }
                }

                return (byte)((row * StoreColumns) + column);
            }
        }

        return null;
    }

    private static bool IsFree(bool[,] occupied, int row, int column, int width, int height)
    {
        for (var r = row; r < row + height; r++)
        {
            for (var c = column; c < column + width; c++)
            {
                if (occupied[r, c])
                {
                    return false;
                }
            }
        }

        return true;
    }

    private Store? GetStore(Player player, NonPlayerCharacter merchant)
    {
        if (this.Configuration is not { } configuration
            || merchant.Definition.Number != configuration.MerchantNumber)
        {
            return null;
        }

        // A changed configuration is a new instance, so the store is created again after a change.
        var gameConfiguration = player.GameContext.Configuration;
        var store = this._store;
        if (store is null || store.Configuration != configuration || store.GameConfiguration != gameConfiguration)
        {
            store = CreateStore(configuration, gameConfiguration);
            this._store = store;
        }

        return store;
    }

    private MedalMerchantConfiguration.MedalPrice? GetPrice(MedalMerchantConfiguration configuration, Item storeItem)
        => this._store is { } store && store.Prices.TryGetValue(storeItem, out var price)
            ? price
            : configuration.GetPrice(storeItem);

    /// <summary>
    /// The items of the store which were created for a configuration, with their prices.
    /// </summary>
    private sealed record Store(
        MedalMerchantConfiguration Configuration,
        GameConfiguration GameConfiguration,
        List<Item> Items,
        Dictionary<Item, MedalMerchantConfiguration.MedalPrice> Prices);
}
