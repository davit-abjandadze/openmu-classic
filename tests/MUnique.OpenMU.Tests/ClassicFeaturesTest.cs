// <copyright file="ClassicFeaturesTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Moq;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.HuntingRewards;
using MUnique.OpenMU.GameLogic.ItemExchange;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.GameLogic.TeamVsTeam;

/// <summary>
/// Tests for the hunting points, the item exchange, the Team vs Team and the prestige for duels.
/// </summary>
[TestFixture]
public class ClassicFeaturesTest
{
    private static readonly DateTime Start = new(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Tests that hunting points are earned from level 400 on, one per minute, up to the daily maximum.
    /// </summary>
    [Test]
    public async Task HuntingPointsAsync()
    {
        var player = await CreatePlayerAsync(HuntingPointStats.All).ConfigureAwait(false);
        var plugIn = new HuntingPointsPlugIn();
        var configuration = new HuntingPointsConfiguration { MaximumHuntingTimePerDay = TimeSpan.FromMinutes(30) };

        player.Attributes![Stats.Level] = 399;
        Hunt(plugIn, player, configuration, Start, 10);
        Assert.That(player.Attributes[HuntingPointStats.Points], Is.EqualTo(0), "below the minimum level");

        player.Attributes[Stats.Level] = 400;
        // 10 minutes of hunting, plus the pause since the last kill, which counts with up to one minute.
        Hunt(plugIn, player, configuration, Start.AddHours(1), 10);
        Assert.That(player.Attributes[HuntingPointStats.Points], Is.EqualTo(11));

        Hunt(plugIn, player, configuration, Start.AddHours(1).AddMinutes(11), 60);
        Assert.That(player.Attributes[HuntingPointStats.Points], Is.EqualTo(30), "daily maximum of 30 minutes");

        Hunt(plugIn, player, configuration, Start.AddDays(1), 5);
        Assert.That(player.Attributes[HuntingPointStats.Points], Is.EqualTo(36), "next day");
    }

    /// <summary>
    /// Tests that the item exchange selects only not equipped excellent items with the minimum drop level, the cheapest first.
    /// </summary>
    [Test]
    public async Task ItemExchangeSelectsItemsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var configuration = new ItemExchangeConfiguration();
        var cheap = await AddItemAsync(player, dropLevel: 45, excellent: true, slot: 20).ConfigureAwait(false);
        var middle = await AddItemAsync(player, dropLevel: 50, excellent: true, slot: 22).ConfigureAwait(false);
        await AddItemAsync(player, dropLevel: 60, excellent: true, slot: 24).ConfigureAwait(false);
        var expensive = await AddItemAsync(player, dropLevel: 70, excellent: true, slot: 26).ConfigureAwait(false);
        await AddItemAsync(player, dropLevel: 30, excellent: true, slot: 28).ConfigureAwait(false);
        await AddItemAsync(player, dropLevel: 50, excellent: false, slot: 30).ConfigureAwait(false);
        await AddItemAsync(player, dropLevel: 45, excellent: true, slot: InventoryConstants.ArmorSlot).ConfigureAwait(false);

        var selected = ItemExchangeChatCommandPlugIn.SelectItems(player, configuration);

        Assert.That(selected, Has.Count.EqualTo(3));
        Assert.That(selected, Does.Contain(cheap).And.Contain(middle).And.Not.Contain(expensive));
        Assert.That(selected.All(i => i.ItemSlot > InventoryConstants.LastEquippableItemSlotIndex), Is.True);
    }

    /// <summary>
    /// Tests that the new item of the exchange gets at least one excellent option and never more than three.
    /// </summary>
    [Test]
    public void ItemExchangeAddsOptions()
    {
        var excellentOptions = Enumerable.Range(0, 6).Select(_ => new IncreasableItemOption { OptionType = ItemOptionTypes.Excellent }).ToList();
        var optionDefinition = new Mock<ItemOptionDefinition>();
        optionDefinition.Setup(d => d.PossibleOptions).Returns(excellentOptions);
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Setup(d => d.PossibleItemOptions).Returns([optionDefinition.Object]);
        var configuration = new ItemExchangeConfiguration { ThreeOptionsChance = 100 };

        for (var i = 0; i < 20; i++)
        {
            var item = CreateItem(definition.Object);
            ItemExchangeChatCommandPlugIn.AddRandomOptions(item, configuration, () => new ItemOptionLink());
            Assert.That(item.ItemOptions.Count(o => object.Equals(o.ItemOption?.OptionType, ItemOptionTypes.Excellent)), Is.EqualTo(3));
        }

        configuration = new ItemExchangeConfiguration { TwoOptionsChance = 0, ThreeOptionsChance = 0 };
        var single = CreateItem(definition.Object);
        ItemExchangeChatCommandPlugIn.AddRandomOptions(single, configuration, () => new ItemOptionLink());
        Assert.That(single.ItemOptions.Count(o => object.Equals(o.ItemOption?.OptionType, ItemOptionTypes.Excellent)), Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that the teams of the Team vs Team are balanced by strength, and the winner and its MVP are determined by the kills.
    /// </summary>
    [Test]
    public async Task TeamVsTeamTeamsAndWinnerAsync()
    {
        var first = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        first.GameContext.Configuration.MaximumLevel = 400;
        var players = new List<Player> { first };
        for (var i = 1; i < 4; i++)
        {
            players.Add(await PlayerTestHelper.CreatePlayerAsync(first.GameContext).ConfigureAwait(false));
        }

        players[0].Attributes![Stats.Level] = 400;
        players[1].Attributes![Stats.Level] = 300;
        players[2].Attributes![Stats.Level] = 200;
        players[3].Attributes![Stats.Level] = 100;

        var teams = TeamVsTeamPlugIn.SplitIntoTeams(players);

        Assert.That(teams.Values.Count(t => t == 0), Is.EqualTo(2));
        Assert.That(teams[players[0]], Is.Not.EqualTo(teams[players[1]]), "the two strongest are in different teams");

        var winningTeam = teams[players[1]];
        var kills = new Dictionary<Player, int> { [players[0]] = 3, [players[1]] = 2 };
        foreach (var player in players.Where(p => teams[p] == winningTeam && p != players[1]))
        {
            kills[player] = 4;
        }

        var result = TeamVsTeamPlugIn.DetermineWinner(teams, kills);
        Assert.That(result?.Team, Is.EqualTo(winningTeam));
        Assert.That(teams[result!.Value.MostValuablePlayer], Is.EqualTo(winningTeam));
        Assert.That(kills[result.Value.MostValuablePlayer], Is.EqualTo(4));

        Assert.That(TeamVsTeamPlugIn.DetermineWinner(teams, new Dictionary<Player, int>()), Is.Null, "draw");
    }

    /// <summary>
    /// Tests that a won duel gives prestige, but not endlessly against the same loser.
    /// </summary>
    [Test]
    public async Task DuelVictoryGivesPrestigeAsync()
    {
        var winner = await CreatePlayerAsync(PvpStats.All).ConfigureAwait(false);
        var loser = await PlayerTestHelper.CreatePlayerAsync(winner.GameContext).ConfigureAwait(false);
        var plugIn = new PvpSpotsPlugIn { Configuration = new PvpSpotsConfiguration { MaximumKillsOfSameVictim = 2 } };

        for (var i = 0; i < 4; i++)
        {
            await plugIn.DuelWonAsync(winner, loser).ConfigureAwait(false);
        }

        Assert.That(winner.Attributes![PvpStats.WeeklyPrestige], Is.EqualTo(20));
        Assert.That(winner.Attributes[PvpStats.TotalPrestige], Is.EqualTo(20));
    }

    private static void Hunt(HuntingPointsPlugIn plugIn, Player player, HuntingPointsConfiguration configuration, DateTime from, int minutes)
    {
        for (var minute = 0; minute <= minutes; minute++)
        {
            plugIn.AddHuntingTime(player, player.Attributes!, configuration, from.AddMinutes(minute));
        }
    }

    private static async ValueTask<Player> CreatePlayerAsync(IEnumerable<MUnique.OpenMU.AttributeSystem.AttributeDefinition> attributes)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        foreach (var attribute in attributes)
        {
            player.Attributes!.TryAddStatAttribute(new MUnique.OpenMU.AttributeSystem.StatAttribute(attribute, 0));
        }

        return player;
    }

    private static async ValueTask<Item> AddItemAsync(Player player, byte dropLevel, bool excellent, byte slot)
    {
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Setup(d => d.PossibleItemOptions).Returns(new List<ItemOptionDefinition>());
        definition.Object.DropLevel = dropLevel;
        definition.Object.Width = 1;
        definition.Object.Height = 1;
        var item = CreateItem(definition.Object);
        if (excellent)
        {
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = new IncreasableItemOption { OptionType = ItemOptionTypes.Excellent } });
        }

        await player.Inventory!.AddItemAsync(slot, item).ConfigureAwait(false);
        return item;
    }

    private static Item CreateItem(ItemDefinition definition)
    {
        var itemMock = new Mock<Item>();
        itemMock.SetupAllProperties();
        itemMock.Setup(i => i.ItemOptions).Returns(new List<ItemOptionLink>());
        itemMock.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        var item = itemMock.Object;
        item.Definition = definition;
        return item;
    }
}
