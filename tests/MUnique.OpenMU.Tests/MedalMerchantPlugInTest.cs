// <copyright file="MedalMerchantPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Tests the <see cref="MedalMerchantPlugIn"/> together with the <see cref="BuyNpcItemAction"/>.
/// </summary>
[TestFixture]
public class MedalMerchantPlugInTest
{
    /// <summary>
    /// The slot of the Box of Kundun +5, which is the second item of the default configuration.
    /// </summary>
    private const byte StoreSlot = 1;

    private const byte BoxOfKundun5Level = 12;

    private const int BoxOfKundun5Price = 15;

    private const long Money = 1_000;

    private MedalMerchantPlugIn _plugIn = null!;

    /// <summary>
    /// Tests that the merchant sells only the Box of Kundun +4 for 10 and the +5 for 15 silver medals.
    /// </summary>
    [Test]
    public void DefaultPricesOfBoxesOfKundun()
    {
        var prices = MedalMerchantConfiguration.CreateDefault().Prices;

        Assert.That(prices.Select(p => (int)p.ItemLevel), Is.EqualTo(new[] { 11, 12 }));
        Assert.That(prices.Select(p => p.SilverMedals), Is.EqualTo(new[] { 10, 15 }));
        Assert.That(prices.All(p => p.ItemGroup == MedalItems.Group && p.ItemNumber == MedalItems.Number), Is.True);
    }

    /// <summary>
    /// Tests that a box is bought for silver medals, without paying zen.
    /// </summary>
    [Test]
    public async ValueTask BoxIsBoughtForSilverMedalsAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 20).ConfigureAwait(false);

        await new BuyNpcItemAction().BuyItemAsync(player, StoreSlot).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Select(i => (int)i.Level), Is.EqualTo(new[] { (int)BoxOfKundun5Level }));
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(20 - BoxOfKundun5Price));
        Assert.That(player.Money, Is.EqualTo(Money));
    }

    /// <summary>
    /// Tests that the purchase is refused without enough silver medals.
    /// </summary>
    [Test]
    public async ValueTask PurchaseWithoutEnoughSilverMedalsIsRefusedAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: BoxOfKundun5Price - 1).ConfigureAwait(false);

        await new BuyNpcItemAction().BuyItemAsync(player, StoreSlot).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(BoxOfKundun5Price - 1));
        Assert.That(player.Money, Is.EqualTo(Money));
        Mock.Get(player.ViewPlugIns.GetPlugIn<IBuyNpcItemFailedPlugIn>()!).Verify(p => p.BuyNpcItemFailedAsync(), Times.Once);
    }

    /// <summary>
    /// Tests that the medal items stay in the inventory when they and the deposited medals aren't enough for the purchase.
    /// </summary>
    [Test]
    public async ValueTask MedalItemsStayWhenNotEnoughAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 5).ConfigureAwait(false);
        await AddMedalItemsAsync(player, MedalItems.SilverMedalLevel, 3).ConfigureAwait(false);
        await AddMedalItemsAsync(player, MedalItems.GoldMedalLevel, 1).ConfigureAwait(false);

        await new BuyNpcItemAction().BuyItemAsync(player, StoreSlot).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Count(i => i.Level == MedalItems.SilverMedalLevel), Is.EqualTo(3));
        Assert.That(player.Inventory.Items.Count(i => i.Level == MedalItems.GoldMedalLevel), Is.EqualTo(1));
        Assert.That(player.Inventory.Items.Any(i => i.Level == BoxOfKundun5Level), Is.False);
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(5));
        Assert.That(player.Attributes[MedalStats.GoldMedals], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the deposited medals pay first, and only the missing silver medal items are removed from the inventory.
    /// </summary>
    [Test]
    public async ValueTask SilverMedalItemsPayTheRestAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 10).ConfigureAwait(false);
        await AddMedalItemsAsync(player, MedalItems.SilverMedalLevel, 7).ConfigureAwait(false);
        await AddMedalItemsAsync(player, MedalItems.GoldMedalLevel, 1).ConfigureAwait(false);

        await new BuyNpcItemAction().BuyItemAsync(player, StoreSlot).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Count(i => i.Level == BoxOfKundun5Level), Is.EqualTo(1));
        Assert.That(player.Inventory.Items.Count(i => i.Level == MedalItems.SilverMedalLevel), Is.EqualTo(2));
        Assert.That(player.Inventory.Items.Count(i => i.Level == MedalItems.GoldMedalLevel), Is.EqualTo(1));
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the merchant offers the configured items instead of the items of its merchant store,
    /// and that an item of its merchant store without a price isn't offered.
    /// </summary>
    [Test]
    public async ValueTask ConfiguredItemsAreOfferedAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 0, storeItemLevel: 7).ConfigureAwait(false);

        var items = player.GetOfferedItems(player.OpenedNpc!)!;

        Assert.That(items.Select(i => ((int)i.ItemSlot, (int)i.Level)), Is.EqualTo(new[] { (0, 11), (1, 12) }));
    }

    /// <summary>
    /// Tests that an item which is added to the configuration while the server is running is offered and sold
    /// the next time the store is opened, and is placed in the first free slot where it fits.
    /// </summary>
    [Test]
    public async ValueTask AddedItemIsSoldAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 30).ConfigureAwait(false);
        var sword = CreateDefinition(0, 1, width: 2, height: 3);
        player.GameContext.Configuration.Items.Add(sword);
        var plugIn = this._plugIn;
        var configuration = MedalMerchantConfiguration.CreateDefault();
        configuration.Prices.Add(new MedalMerchantConfiguration.MedalPrice { ItemGroup = 0, ItemNumber = 1, ItemLevel = 9, SilverMedals = 25 });
        _ = player.GetOfferedItems(player.OpenedNpc!);

        plugIn.Configuration = configuration;
        var items = player.GetOfferedItems(player.OpenedNpc!)!;
        await new BuyNpcItemAction().BuyItemAsync(player, 2).ConfigureAwait(false);

        Assert.That(items.Select(i => (int)i.ItemSlot), Is.EqualTo(new[] { 0, 1, 2 }));
        Assert.That(player.Inventory!.Items.Single().Definition, Is.SameAs(sword));
        Assert.That(player.Inventory.Items.Single().Level, Is.EqualTo(9));
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(5));
        Assert.That(player.Money, Is.EqualTo(Money));
    }

    /// <summary>
    /// Tests that an item whose options aren't allowed by its definition isn't offered.
    /// </summary>
    [Test]
    public async ValueTask ItemWithInvalidOptionsIsNotOfferedAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 0).ConfigureAwait(false);
        var plugIn = this._plugIn;
        var configuration = MedalMerchantConfiguration.CreateDefault();
        configuration.Prices.Add(new MedalMerchantConfiguration.MedalPrice { ItemGroup = MedalItems.Group, ItemNumber = MedalItems.Number, Luck = true, SilverMedals = 1 });
        plugIn.Configuration = configuration;

        var items = player.GetOfferedItems(player.OpenedNpc!)!;

        Assert.That(items, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Tests that an empty slot of the merchant can't be bought.
    /// </summary>
    [Test]
    public async ValueTask EmptySlotIsRefusedAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 100).ConfigureAwait(false);

        await new BuyNpcItemAction().BuyItemAsync(player, 5).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items, Is.Empty);
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(100));
        Assert.That(player.Money, Is.EqualTo(Money));
    }

    /// <summary>
    /// Tests that the purchases of other merchants are not changed.
    /// </summary>
    [Test]
    public async ValueTask OtherMerchantsAreNotChangedAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 100, merchantNumber: 230).ConfigureAwait(false);
        var plugIn = new MedalMerchantPlugIn { Configuration = MedalMerchantConfiguration.CreateDefault() };
        var args = new NpcItemPurchaseArgs(player.OpenedNpc!.Definition.MerchantStore!.Items.Single());

        await plugIn.BeforePurchaseAsync(player, player.OpenedNpc, args).ConfigureAwait(false);

        Assert.That((args.Cancel, args.Price), Is.EqualTo((false, (long?)null)));
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(100));
    }

    /// <summary>
    /// Tests that a silver medal item can't be dropped from the inventory, but a gold medal item can.
    /// </summary>
    [Test]
    public async ValueTask SilverMedalCantBeDroppedAsync()
    {
        var player = await CreatePlayerAsync(silverMedals: 0).ConfigureAwait(false);
        await AddMedalItemsAsync(player, MedalItems.SilverMedalLevel, 1).ConfigureAwait(false);
        await AddMedalItemsAsync(player, MedalItems.GoldMedalLevel, 1).ConfigureAwait(false);
        var silver = player.Inventory!.Items.Single(i => i.Level == MedalItems.SilverMedalLevel);
        var gold = player.Inventory.Items.Single(i => i.Level == MedalItems.GoldMedalLevel);
        var target = player.Position;
        player.CurrentMap!.Terrain.WalkMap[target.X, target.Y] = true;

        await new DropItemAction().DropItemAsync(player, silver.ItemSlot, target).ConfigureAwait(false);
        await new DropItemAction().DropItemAsync(player, gold.ItemSlot, target).ConfigureAwait(false);

        Assert.That(player.Inventory.Items, Is.EqualTo(new[] { silver }));
        Mock.Get(player.ViewPlugIns.GetPlugIn<IItemDropResultPlugIn>()!).Verify(p => p.ItemDropResultAsync(silver.ItemSlot, false), Times.Once);
    }

    private static async ValueTask AddMedalItemsAsync(Player player, byte level, int count)
    {
        var boxOfLuck = player.OpenedNpc!.Definition.MerchantStore!.Items.Single().Definition!;
        for (var i = 0; i < count; i++)
        {
            var slot = (byte)(InventoryConstants.LastEquippableItemSlotIndex + 1 + player.Inventory!.Items.Count());
            await player.Inventory.AddItemAsync(slot, CreateItem(boxOfLuck, level)).ConfigureAwait(false);
        }
    }

    private static Item CreateItem(ItemDefinition definition, byte level)
    {
        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Setup(i => i.ItemOptions).Returns(new List<ItemOptionLink>());
        item.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        item.Object.Definition = definition;
        item.Object.Level = level;
        item.Object.Durability = 1;
        return item.Object;
    }

    private static ItemDefinition CreateDefinition(byte group, short number, byte width, byte height)
    {
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Setup(d => d.PossibleItemOptions).Returns(new List<ItemOptionDefinition>());
        definition.Setup(d => d.PossibleItemSetGroups).Returns(new List<ItemSetGroup>());
        definition.Object.Group = group;
        definition.Object.Number = number;
        definition.Object.Width = width;
        definition.Object.Height = height;
        definition.Object.Durability = 1;
        definition.Object.MaximumItemLevel = 15;
        return definition.Object;
    }

    private async ValueTask<Player> CreatePlayerAsync(int silverMedals, short merchantNumber = MedalMerchantConfiguration.DefaultMerchantNumber, byte storeItemLevel = BoxOfKundun5Level)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        foreach (var attribute in MedalStats.All)
        {
            player.Attributes!.TryAddStatAttribute(new StatAttribute(attribute, 0));
        }

        player.Attributes![MedalStats.SilverMedals] = silverMedals;
        player.Money = (int)Money;
        var plugIn = this._plugIn = new MedalMerchantPlugIn { Configuration = MedalMerchantConfiguration.CreateDefault() };
        player.GameContext.PlugInManager.RegisterPlugInAtPlugInPoint<INpcItemPurchasePlugIn>(plugIn);
        player.GameContext.PlugInManager.RegisterPlugInAtPlugInPoint<IMerchantStoreItemsPlugIn>(plugIn);

        var boxOfLuck = CreateDefinition(MedalItems.Group, MedalItems.Number, width: 1, height: 1);
        player.GameContext.Configuration.Items.Add(boxOfLuck);
        var storeItem = CreateItem(boxOfLuck, storeItemLevel);
        storeItem.ItemSlot = StoreSlot;
        var store = new Mock<ItemStorage>();
        store.SetupAllProperties();
        store.Setup(s => s.Items).Returns(new List<Item> { storeItem });
        player.OpenedNpc = new NonPlayerCharacter(null!, new MonsterDefinition { Number = merchantNumber, MerchantStore = store.Object }, null!);
        return player;
    }
}
