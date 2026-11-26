// <copyright file="PvpSpotsPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Tests for the <see cref="PvpSpotsPlugIn"/> and its <see cref="PvpSpotsConfiguration"/>.
/// </summary>
[TestFixture]
public class PvpSpotsPlugInTest
{
    private static readonly Point InSpot = new(110, 110);
    private static readonly Point OutsideSpot = new(200, 200);

    private PvpSpotsPlugIn _plugIn = null!;
    private Player _killer = null!;
    private Player _victim = null!;
    private Player _helper = null!;

    /// <summary>
    /// Sets up three players on map 0 and the plugin with one spot from 100,100 to 120,120.
    /// </summary>
    [SetUp]
    public async Task SetUpAsync()
    {
        this._plugIn = new PvpSpotsPlugIn
        {
            Configuration = new PvpSpotsConfiguration
            {
                Spots = { new PvpSpotsConfiguration.PvpSpot { Name = "Test", MapNumber = 0, X1 = 100, Y1 = 100, X2 = 120, Y2 = 120 } },
            },
        };

        var gameContext = (await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false)).GameContext;
        gameContext.Configuration.MaximumLevel = 400;
        this._killer = await CreatePlayerAsync(gameContext).ConfigureAwait(false);
        this._victim = await CreatePlayerAsync(gameContext).ConfigureAwait(false);
        this._helper = await CreatePlayerAsync(gameContext).ConfigureAwait(false);
        foreach (var player in new[] { this._killer, this._victim, this._helper })
        {
            player.Attributes![Stats.Level] = 300;
            player.Position = InSpot;
        }
    }

    /// <summary>
    /// Tests the week numbers: a week starts every 7 days from the start.
    /// </summary>
    [Test]
    public void WeekNumbers()
    {
        var configuration = new PvpSpotsConfiguration { WeekStart = new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc) };
        Assert.That(configuration.GetWeekNumber(new DateTime(2026, 11, 15, 20, 0, 0, DateTimeKind.Utc)), Is.EqualTo(1));
        Assert.That(configuration.GetWeekNumber(new DateTime(2026, 11, 22, 19, 59, 0, DateTimeKind.Utc)), Is.EqualTo(1));
        Assert.That(configuration.GetWeekNumber(new DateTime(2026, 11, 22, 20, 0, 0, DateTimeKind.Utc)), Is.EqualTo(2));
    }

    /// <summary>
    /// Tests that a kill in a spot is exempted from the player killer penalty, and outside not.
    /// </summary>
    [Test]
    public void KillInSpotIsExempted()
    {
        Assert.That(IsExempted(this._killer, this._victim), Is.True);

        this._victim.Position = OutsideSpot;
        Assert.That(IsExempted(this._killer, this._victim), Is.False);
    }

    /// <summary>
    /// Tests that a kill in a spot counts a kill with prestige for the killer, a death for the victim and an assist for the helper.
    /// </summary>
    [Test]
    public async Task KillInSpotGivesPrestigeDeathAndAssistAsync()
    {
        var hitInfo = new HitInfo(1, 0, DamageAttributes.Undefined);
        this._plugIn.AttackableGotHit(this._victim, this._helper, hitInfo);
        this._plugIn.AttackableGotHit(this._victim, this._killer, hitInfo);

        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);

        Assert.That(this._killer.Attributes![PvpStats.WeeklyKills], Is.EqualTo(1));
        Assert.That(this._killer.Attributes[PvpStats.WeeklyPrestige], Is.EqualTo(10));
        Assert.That(this._killer.Attributes[PvpStats.TotalPrestige], Is.EqualTo(10));
        Assert.That(this._victim.Attributes![PvpStats.WeeklyDeaths], Is.EqualTo(1));
        Assert.That(this._helper.Attributes![PvpStats.WeeklyAssists], Is.EqualTo(1));
        Assert.That(this._killer.Attributes[PvpStats.WeeklyAssists], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that a kill outside of a spot counts nothing, unless the victim is a player killer, which gives double prestige.
    /// </summary>
    [Test]
    public async Task KillOutsideSpotOnlyCountsForPlayerKillersAsync()
    {
        this._victim.Position = OutsideSpot;
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);
        Assert.That(this._killer.Attributes![PvpStats.WeeklyKills], Is.EqualTo(0));

        this._victim.SelectedCharacter!.State = HeroState.PlayerKiller1stStage;
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);
        Assert.That(this._killer.Attributes[PvpStats.WeeklyKills], Is.EqualTo(1));
        Assert.That(this._killer.Attributes[PvpStats.WeeklyPrestige], Is.EqualTo(20));
    }

    /// <summary>
    /// Tests that a killer outside of the spot doesn't get a spot kill, like it doesn't get the exemption of the player killer penalty.
    /// </summary>
    [Test]
    public async Task KillerOutsideSpotCountsNothingAsync()
    {
        this._killer.Position = OutsideSpot;
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);

        Assert.That(this._killer.Attributes![PvpStats.WeeklyKills], Is.EqualTo(0));
        Assert.That(this._killer.Attributes[PvpStats.WeeklyPrestige], Is.EqualTo(0));
        Assert.That(this._victim.Attributes![PvpStats.WeeklyDeaths], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that killing a member of the own party counts nothing.
    /// </summary>
    [Test]
    public async Task KillOfPartyMemberCountsNothingAsync()
    {
        var party = new Party(new PartyManager(5, new NullLogger<Party>()), 5, new NullLogger<Party>());
        this._killer.Party = party;
        this._victim.Party = party;
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);

        Assert.That(this._killer.Attributes![PvpStats.WeeklyKills], Is.EqualTo(0));
        Assert.That(this._killer.Attributes[PvpStats.WeeklyPrestige], Is.EqualTo(0));
    }

    /// <summary>
    /// Tests that killing a low level character without resets counts nothing, but with resets it counts.
    /// </summary>
    [Test]
    public async Task KillOfLowLevelCharacterCountsOnlyWithResetsAsync()
    {
        this._victim.Attributes![Stats.Level] = this._plugIn.Configuration!.MinimumVictimLevel - 1;
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);
        Assert.That(this._killer.Attributes![PvpStats.WeeklyKills], Is.EqualTo(0));

        this._victim.Attributes[Stats.Resets] = 1;
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);
        Assert.That(this._killer.Attributes[PvpStats.WeeklyKills], Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that a stronger victim gives more prestige.
    /// </summary>
    [Test]
    public async Task StrongerVictimGivesMorePrestigeAsync()
    {
        this._victim.Attributes![Stats.Resets] = 1;
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);
        Assert.That(this._killer.Attributes![PvpStats.WeeklyPrestige], Is.EqualTo(15));
    }

    /// <summary>
    /// Tests that killing the same victim too often doesn't give prestige anymore, but still counts the kill.
    /// </summary>
    [Test]
    public async Task FarmingTheSameVictimGivesNoPrestigeAsync()
    {
        for (var i = 0; i < 5; i++)
        {
            await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);
        }

        Assert.That(this._killer.Attributes![PvpStats.WeeklyKills], Is.EqualTo(5));
        Assert.That(this._killer.Attributes[PvpStats.WeeklyPrestige], Is.EqualTo(30));
    }

    /// <summary>
    /// Tests that the weekly values are reset when a new week started, but the totals are kept.
    /// </summary>
    [Test]
    public async Task WeeklyValuesStartOverInNewWeekAsync()
    {
        await this._plugIn.AttackableGotKilledAsync(this._victim, this._killer).ConfigureAwait(false);
        var configuration = this._plugIn.Configuration!;

        PvpSpotsPlugIn.EnsureCurrentWeek(this._killer, configuration, DateTime.UtcNow.AddDays(7));

        Assert.That(this._killer.Attributes![PvpStats.WeeklyPrestige], Is.EqualTo(0));
        Assert.That(this._killer.Attributes[PvpStats.WeeklyKills], Is.EqualTo(0));
        Assert.That(this._killer.Attributes[PvpStats.TotalPrestige], Is.EqualTo(10));
    }

    private static bool IsExempted(Player attacker, Player defender)
    {
        var plugIn = new PvpSpotsPlugIn
        {
            Configuration = new PvpSpotsConfiguration
            {
                Spots = { new PvpSpotsConfiguration.PvpSpot { Name = "Test", MapNumber = 0, X1 = 100, Y1 = 100, X2 = 120, Y2 = 120 } },
            },
        };
        var args = new PlayerKillPenaltyExemptionArgs();
        plugIn.CheckExemption(attacker, defender, args);
        return args.IsExempted;
    }

    private static async ValueTask<Player> CreatePlayerAsync(IGameContext gameContext)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync(gameContext).ConfigureAwait(false);
        foreach (var attribute in PvpStats.All)
        {
            player.Attributes!.TryAddStatAttribute(new MUnique.OpenMU.AttributeSystem.StatAttribute(attribute, 0));
        }

        return player;
    }
}
