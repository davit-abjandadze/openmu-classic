// <copyright file="MiniGameMedalsPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;

/// <summary>
/// Tests the medal calculation of the <see cref="MiniGameMedalsPlugIn"/>.
/// </summary>
[TestFixture]
public class MiniGameMedalsPlugInTest
{
    private readonly MiniGameMedalsConfiguration _configuration = new();
    private List<Player> _players = null!;

    /// <summary>
    /// Creates five players.
    /// </summary>
    [SetUp]
    public async Task SetUpAsync()
    {
        var first = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        this._players = [first];
        for (var i = 1; i < 5; i++)
        {
            this._players.Add(await PlayerTestHelper.CreatePlayerAsync(first.GameContext).ConfigureAwait(false));
        }

        for (var i = 0; i < this._players.Count; i++)
        {
            this._players[i].SelectedCharacter!.Name = $"Player{i}";
        }
    }

    /// <summary>
    /// Tests that in the Devil Square the 1st place gets four silver medals, the 2nd and 3rd place two,
    /// and a player with at least 50 kills an additional silver medal.
    /// </summary>
    [Test]
    public void DevilSquareMedals()
    {
        var kills = new Dictionary<string, int> { ["Player0"] = 80, ["Player4"] = 50, ["Player3"] = 49 };

        var medals = MiniGameMedalsPlugIn.CalculateMedals(MiniGameType.DevilSquare, this._players, null, this._players, kills, this._configuration);

        Assert.That(medals[this._players[0]], Is.EqualTo(5));
        Assert.That(medals[this._players[1]], Is.EqualTo(2));
        Assert.That(medals[this._players[2]], Is.EqualTo(2));
        Assert.That(medals.ContainsKey(this._players[3]), Is.False);
        Assert.That(medals[this._players[4]], Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that in the Blood Castle the winner gets three silver medals and the other members of its party one.
    /// </summary>
    [Test]
    public void BloodCastleMedals()
    {
        var party = new Party(new PartyManager(5, new NullLogger<Party>()), 5, new NullLogger<Party>());
        this._players[1].Party = party;
        this._players[2].Party = party;

        var medals = MiniGameMedalsPlugIn.CalculateMedals(MiniGameType.BloodCastle, this._players, this._players[1], this._players, new Dictionary<string, int>(), this._configuration);

        Assert.That(medals[this._players[1]], Is.EqualTo(3));
        Assert.That(medals[this._players[2]], Is.EqualTo(1));
        Assert.That(medals, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Tests that a Blood Castle without a winner gives no medals for the ranking.
    /// </summary>
    [Test]
    public void BloodCastleWithoutWinnerGivesNoMedals()
    {
        var medals = MiniGameMedalsPlugIn.CalculateMedals(MiniGameType.BloodCastle, this._players, null, this._players, new Dictionary<string, int>(), this._configuration);
        Assert.That(medals, Is.Empty);
    }

    /// <summary>
    /// Tests that a reward of zero silver medals gives the player no entry.
    /// </summary>
    [Test]
    public void ZeroMedalsAreLeftOut()
    {
        var configuration = new MiniGameMedalsConfiguration { DevilSquareWinnerSilverMedals = 0, KillsForSilverMedal = 0 };

        var medals = MiniGameMedalsPlugIn.CalculateMedals(MiniGameType.DevilSquare, this._players, null, this._players, new Dictionary<string, int>(), configuration);

        Assert.That(medals.Keys, Is.EquivalentTo(new[] { this._players[1], this._players[2] }));
    }

    /// <summary>
    /// Tests that the silver medals are put into the inventory as items, and that the winner gets a win.
    /// </summary>
    [Test]
    public async Task MedalsArePutIntoTheInventoryAsync()
    {
        var player = await CreateMedalPlayerAsync().ConfigureAwait(false);

        await MiniGameMedalsPlugIn.AwardAsync(player, MiniGameType.DevilSquare, 4, true).ConfigureAwait(false);

        Assert.That(player.Inventory!.Items.Count(MedalItems.IsSilverMedal), Is.EqualTo(4));
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(0));
        Assert.That(player.Attributes[MedalStats.DevilSquareWins], Is.EqualTo(1));
        Assert.That(player.Attributes[MedalStats.BloodCastleWins], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the silver medals which don't fit into the inventory are added to the silver medals of the character.
    /// </summary>
    [Test]
    public async Task MedalsWhichDontFitAreAddedToTheCharacterAsync()
    {
        var player = await CreateMedalPlayerAsync().ConfigureAwait(false);
        var filler = CreateDefinition(0, 1);
        for (var i = 0; i < 500 && await player.Inventory!.AddItemAsync(CreateItem(player, filler)).ConfigureAwait(false); i++)
        {
            // Fills the inventory.
        }

        await player.Inventory!.RemoveItemAsync(player.Inventory.Items.Last(i => i.Definition == filler)).ConfigureAwait(false);

        await MiniGameMedalsPlugIn.AwardAsync(player, MiniGameType.BloodCastle, 3, false).ConfigureAwait(false);

        Assert.That(player.Inventory.Items.Count(MedalItems.IsSilverMedal), Is.EqualTo(1));
        Assert.That(player.Attributes![MedalStats.SilverMedals], Is.EqualTo(2));
        Assert.That(player.Attributes[MedalStats.BloodCastleWins], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that the winner, which counts as a win, is the 1st place of the Devil Square and the winner of the Blood Castle.
    /// </summary>
    [Test]
    public void Winner()
    {
        Assert.That(MiniGameMedalsPlugIn.GetWinner(MiniGameType.DevilSquare, this._players, null), Is.SameAs(this._players[0]));
        Assert.That(MiniGameMedalsPlugIn.GetWinner(MiniGameType.DevilSquare, [], null), Is.Null);
        Assert.That(MiniGameMedalsPlugIn.GetWinner(MiniGameType.BloodCastle, this._players, this._players[2]), Is.SameAs(this._players[2]));
        Assert.That(MiniGameMedalsPlugIn.GetWinner(MiniGameType.BloodCastle, this._players, null), Is.Null);
    }

    private static async ValueTask<Player> CreateMedalPlayerAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        foreach (var attribute in MedalStats.All)
        {
            player.Attributes!.TryAddStatAttribute(new StatAttribute(attribute, 0));
        }

        player.GameContext.Configuration.Items.Add(CreateDefinition(MedalItems.Group, MedalItems.Number));
        return player;
    }

    private static ItemDefinition CreateDefinition(byte group, short number)
    {
        var definition = new Mock<ItemDefinition>();
        definition.SetupAllProperties();
        definition.Setup(d => d.PossibleItemOptions).Returns(new List<ItemOptionDefinition>());
        definition.Setup(d => d.PossibleItemSetGroups).Returns(new List<ItemSetGroup>());
        definition.Object.Group = group;
        definition.Object.Number = number;
        definition.Object.Width = 1;
        definition.Object.Height = 1;
        definition.Object.Durability = 1;
        definition.Object.MaximumItemLevel = 15;
        return definition.Object;
    }

    private static Item CreateItem(Player player, ItemDefinition definition)
    {
        var item = player.PersistenceContext.CreateNew<Item>();
        item.Definition = definition;
        item.Durability = 1;
        return item;
    }
}
