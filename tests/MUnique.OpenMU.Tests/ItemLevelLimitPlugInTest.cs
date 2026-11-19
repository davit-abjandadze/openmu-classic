// <copyright file="ItemLevelLimitPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views.NPC;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests that the <see cref="SimpleItemCraftingHandler"/> doesn't upgrade an item above the limit
/// of the <see cref="ItemLevelLimitPlugIn"/>, or above the maximum level of the item definition.
/// </summary>
[TestFixture]
public class ItemLevelLimitPlugInTest
{
    /// <summary>
    /// Tests that an upgrade to the limit is allowed.
    /// </summary>
    [Test]
    public async ValueTask UpgradeUpToTheLimitIsAllowedAsync()
    {
        var result = await TryUpgradeAsync(itemLevel: 10, limit: 11, definitionMaximum: 15).ConfigureAwait(false);
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Tests that an upgrade above the limit is refused.
    /// </summary>
    [Test]
    public async ValueTask UpgradeAboveTheLimitIsRefusedAsync()
    {
        var result = await TryUpgradeAsync(itemLevel: 11, limit: 11, definitionMaximum: 15).ConfigureAwait(false);
        Assert.That(result, Is.EqualTo(CraftingResult.InvalidItemLevel));
    }

    /// <summary>
    /// Tests that a raised limit allows the upgrade again.
    /// </summary>
    [Test]
    public async ValueTask RaisedLimitAllowsTheUpgradeAsync()
    {
        var result = await TryUpgradeAsync(itemLevel: 12, limit: 13, definitionMaximum: 15).ConfigureAwait(false);
        Assert.That(result, Is.Null);
    }

    /// <summary>
    /// Tests that an upgrade above the maximum level of the item definition is refused, even without the plugin.
    /// </summary>
    [Test]
    public async ValueTask UpgradeAboveTheDefinitionMaximumIsRefusedAsync()
    {
        var result = await TryUpgradeAsync(itemLevel: 13, limit: null, definitionMaximum: 13).ConfigureAwait(false);
        Assert.That(result, Is.EqualTo(CraftingResult.InvalidItemLevel));
    }

    private static async ValueTask<CraftingResult?> TryUpgradeAsync(byte itemLevel, byte? limit, byte definitionMaximum)
    {
        var plugInConfigurations = limit is null
            ? new List<PlugInConfiguration>()
            : [new PlugInConfiguration { TypeId = typeof(ItemLevelLimitPlugIn).GUID, IsActive = true }];
        var gameContext = GameContextTestHelper.CreateGameContext(plugInConfigurations);
        if (limit is { } maximumItemLevel)
        {
            gameContext.FeaturePlugIns.GetPlugIn<ItemLevelLimitPlugIn>()!.Configuration = new ItemLevelLimitConfiguration { MaximumItemLevel = maximumItemLevel };
        }

        var player = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        var definition = new Persistence.BasicModel.ItemDefinition { Width = 1, Height = 1, MaximumItemLevel = definitionMaximum, Durability = 1 };
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Level = itemLevel;
        item.Durability = definition.Durability;
        await player.TemporaryStorage!.AddItemAsync(item).ConfigureAwait(false);

        var settings = new Persistence.BasicModel.SimpleCraftingSettings { SuccessPercent = 50 };
        settings.RequiredItems.Add(new Persistence.BasicModel.ItemCraftingRequiredItem { MinimumAmount = 1, MaximumAmount = 1, MinimumItemLevel = itemLevel, MaximumItemLevel = itemLevel, Reference = 1 });
        settings.ResultItems.Add(new Persistence.BasicModel.ItemCraftingResultItem { Reference = 1, AddLevel = 1 });

        return new SimpleItemCraftingHandler(settings).TryGetRequiredItems(player, out _, out _);
    }
}
