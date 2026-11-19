// <copyright file="MiniGameMedalsPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;

/// <summary>
/// Tests the medal calculation of the <see cref="MiniGameMedalsPlugIn"/>.
/// </summary>
[TestFixture]
public class MiniGameMedalsPlugInTest
{
    private readonly MiniGameMedalsConfiguration _configuration = MiniGameMedalsConfiguration.CreateDefault();
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
    /// Tests that in the Devil Square the 1st place gets a gold medal, the 2nd and 3rd place two silver medals,
    /// and a player with at least 50 kills an additional silver medal.
    /// </summary>
    [Test]
    public void DevilSquareMedals()
    {
        var kills = new Dictionary<string, int> { ["Player0"] = 80, ["Player4"] = 50, ["Player3"] = 49 };

        var medals = MiniGameMedalsPlugIn.CalculateMedals(MiniGameType.DevilSquare, this._players, null, this._players, kills, this._configuration);

        Assert.That(medals[this._players[0]], Is.EqualTo((1, 1)));
        Assert.That(medals[this._players[1]], Is.EqualTo((0, 2)));
        Assert.That(medals[this._players[2]], Is.EqualTo((0, 2)));
        Assert.That(medals.ContainsKey(this._players[3]), Is.False);
        Assert.That(medals[this._players[4]], Is.EqualTo((0, 1)));
    }

    /// <summary>
    /// Tests that in the Blood Castle the winner gets a gold medal and the other members of its party a silver medal.
    /// </summary>
    [Test]
    public void BloodCastleMedals()
    {
        var party = new Party(new PartyManager(5, new NullLogger<Party>()), 5, new NullLogger<Party>());
        this._players[1].Party = party;
        this._players[2].Party = party;

        var medals = MiniGameMedalsPlugIn.CalculateMedals(MiniGameType.BloodCastle, this._players, this._players[1], this._players, new Dictionary<string, int>(), this._configuration);

        Assert.That(medals[this._players[1]], Is.EqualTo((1, 0)));
        Assert.That(medals[this._players[2]], Is.EqualTo((0, 1)));
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
}
