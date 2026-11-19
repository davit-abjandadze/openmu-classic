// <copyright file="HuntingJewelPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.HuntingRewards;

/// <summary>
/// Tests for the <see cref="HuntingJewelPlugIn"/>.
/// </summary>
[TestFixture]
public class HuntingJewelPlugInTest
{
    private static readonly DateTime Start = new(2026, 11, 19, 10, 0, 0, DateTimeKind.Utc);

    private HuntingJewelPlugIn _plugIn = null!;
    private HuntingJewelConfiguration _configuration = null!;
    private Player _player = null!;
    private ItemDefinition _jewel = null!;

    /// <summary>
    /// Sets up a player with the hunting attributes and a jewel definition.
    /// </summary>
    [SetUp]
    public async Task SetUpAsync()
    {
        this._player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        foreach (var attribute in HuntingJewelStats.All)
        {
            this._player.Attributes!.TryAddStatAttribute(new MUnique.OpenMU.AttributeSystem.StatAttribute(attribute, 0));
        }

        this._jewel = new ItemDefinition { Name = "Jewel of Bless", Group = 14, Number = 13, Width = 1, Height = 1, Durability = 1 };
        this._player.GameContext.Configuration.Items.Add(this._jewel);
        this._configuration = new HuntingJewelConfiguration { Jewels = ["14/13"], MaximumJewelsPerDay = 2 };
        this._plugIn = new HuntingJewelPlugIn { Configuration = this._configuration };
    }

    /// <summary>
    /// Tests that 20 minutes of hunting with a kill per minute give a jewel.
    /// </summary>
    [Test]
    public async Task TwentyMinutesOfHuntingGiveAJewelAsync()
    {
        await this.HuntAsync(Start, minutes: 19).ConfigureAwait(false);
        Assert.That(this.JewelCount, Is.EqualTo(0));

        await this.HuntAsync(Start.AddMinutes(19), minutes: 1).ConfigureAwait(false);
        Assert.That(this.JewelCount, Is.EqualTo(1));
        Assert.That(this._player.Attributes![HuntingJewelStats.JewelsOfDay], Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that idle time between kills only counts up to the maximum time between kills.
    /// </summary>
    [Test]
    public async Task IdleTimeDoesNotCountAsync()
    {
        await this._plugIn.AddHuntingTimeAsync(this._player, this._player.Attributes!, this._configuration, Start).ConfigureAwait(false);
        await this._plugIn.AddHuntingTimeAsync(this._player, this._player.Attributes!, this._configuration, Start.AddHours(2)).ConfigureAwait(false);

        Assert.That(this._player.Attributes![HuntingJewelStats.HuntingSeconds], Is.EqualTo(60));
        Assert.That(this.JewelCount, Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that there are no more jewels after the daily maximum, until the next day.
    /// </summary>
    [Test]
    public async Task DailyMaximumAsync()
    {
        await this.HuntAsync(Start, minutes: 60).ConfigureAwait(false);
        Assert.That(this.JewelCount, Is.EqualTo(2));

        var nextDay = Start.AddDays(1);
        await this.HuntAsync(nextDay, minutes: 20).ConfigureAwait(false);
        Assert.That(this.JewelCount, Is.EqualTo(3));
    }

    private int JewelCount => this._player.Inventory!.Items.Count(i => i.Definition == this._jewel);

    private async Task HuntAsync(DateTime from, int minutes)
    {
        for (var minute = 0; minute <= minutes; minute++)
        {
            await this._plugIn.AddHuntingTimeAsync(this._player, this._player.Attributes!, this._configuration, from.AddMinutes(minute)).ConfigureAwait(false);
        }
    }
}
