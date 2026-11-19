// <copyright file="ClassicDataInitializationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests the classic (Season 1 content with DK, DW and Elf) data initialization for the Season 6 client.
/// </summary>
[TestFixture]
internal class ClassicDataInitializationTest
{
    private GameConfiguration _gameConfiguration = null!;
    private IContext _context = null!;

    /// <summary>
    /// Creates the classic data once for all tests.
    /// </summary>
    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new ClassicDataInitialization(contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        this._context = contextProvider.CreateNewContext();
        this._gameConfiguration = (await this._context.GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
    }

    /// <summary>
    /// Disposes the context.
    /// </summary>
    [OneTimeTearDown]
    public void TearDown()
    {
        this._context.Dispose();
    }

    /// <summary>
    /// Tests that only Dark Wizard, Dark Knight and Fairy Elf can be created, and only they and their second classes are active.
    /// </summary>
    [Test]
    public void OnlyClassicClassesAreActive()
    {
        var active = this._gameConfiguration.CharacterClasses.Where(c => c.IsActive).Select(c => (int)c.Number).Order();
        var creatable = this._gameConfiguration.CharacterClasses.Where(c => c.CanGetCreated).Select(c => (int)c.Number).Order();

        Assert.That(active, Is.EqualTo(new[] { 0, 2, 4, 6, 8, 10 }));
        Assert.That(creatable, Is.EqualTo(new[] { 0, 4, 8 }));
    }

    /// <summary>
    /// Tests that no content newer than Season 1 is active.
    /// </summary>
    [Test]
    public void NoNewerContentIsActive()
    {
        var target = ClassicContentRestriction.TargetVersion;
        Assert.That(this._gameConfiguration.Maps.Where(m => m.IsActive && m.IntroducedIn > target), Is.Empty);
        Assert.That(this._gameConfiguration.Monsters.Where(m => m.IsActive && m.IntroducedIn > target), Is.Empty);
        Assert.That(this._gameConfiguration.Items.Where(m => m.IsActive && m.IntroducedIn > target), Is.Empty);
        Assert.That(this._gameConfiguration.MiniGameDefinitions.Where(m => m.IsActive && m.IntroducedIn > target), Is.Empty);
        Assert.That(this._gameConfiguration.Maps.Single(m => m.Number == 0).IsActive, Is.True, "Lorencia");
        Assert.That(this._gameConfiguration.Maps.Single(m => m.Number == 10).IsActive, Is.True, "Icarus");
    }

    /// <summary>
    /// Tests that items which only inactive classes can wear, and non-classic items of an unknown version, are inactive,
    /// while the classic items stay active.
    /// </summary>
    [Test]
    public void NonClassicItemsAreInactive()
    {
        bool IsActive(byte group, short number) => this._gameConfiguration.Items.Single(i => i.Group == group && i.Number == number).IsActive;

        Assert.That(IsActive(0, 0), Is.True, "Kris");
        Assert.That(IsActive(13, 21), Is.True, "Ring of Fire");
        Assert.That(IsActive(13, 47), Is.True, "Blood Castle Ticket");
        Assert.That(IsActive(14, 13), Is.True, "Jewel of Bless");
        Assert.That(IsActive(2, 8), Is.False, "Battle Scepter (Dark Lord)");
        Assert.That(IsActive(8, 66), Is.False, "Adamantine Armor");
        Assert.That(IsActive(14, 72), Is.False, "Scroll of Quickness");
        Assert.That(IsActive(14, 42), Is.False, "Jewel of Harmony");
        Assert.That(this._gameConfiguration.Monsters.Single(m => m.Number == 78).IsActive, Is.True, "Golden Goblin");
    }

    /// <summary>
    /// Tests that items can't be upgraded above +13.
    /// </summary>
    [Test]
    public void ItemsAreLimitedToLevel13()
    {
        Assert.That(this._gameConfiguration.Items.Max(i => i.MaximumItemLevel), Is.LessThanOrEqualTo(ClassicContentRestriction.MaximumItemLevel));
        var goblinCraftings = this._gameConfiguration.Monsters.Single(m => m.Number == 238).ItemCraftings;
        Assert.That(goblinCraftings.Select(c => (int)c.Number), Does.Contain(23).And.Not.Contain(49).And.Not.Contain(50));
    }

    /// <summary>
    /// Tests that the reset feature and the experience rate by resets are active and configured.
    /// </summary>
    [Test]
    public void ResetPlugInsAreActiveAndConfigured()
    {
        var reset = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.Resets.ResetFeaturePlugIn).GUID);
        var experience = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.Resets.ResetExperienceRatePlugIn).GUID);
        Assert.That(reset.IsActive, Is.True);
        Assert.That(experience.IsActive, Is.True);
        Assert.That(reset.CustomConfiguration?.Replace(" ", string.Empty), Does.Contain("\"RequiredLevelIncreasePerReset\":10").And.Contain("\"ResetLimit\":55"));
        Assert.That(experience.CustomConfiguration, Does.Contain("540"));
    }

    /// <summary>
    /// Tests that the PvP plugin is active with two spots on each of the eight classic maps,
    /// and that the PvP attributes are stat attributes of all classes, so that they are persisted.
    /// </summary>
    [Test]
    public void PvpSpotsAreActiveAndConfigured()
    {
        var pvp = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PvP.PvpSpotsPlugIn).GUID);
        Assert.That(pvp.IsActive, Is.True);
        var spots = MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic.ClassicPvpSpots.Create(this._gameConfiguration).ToList();
        foreach (var spot in spots)
        {
            TestContext.Out.WriteLine(spot);
        }

        Assert.That(spots, Has.Count.EqualTo(16));
        Assert.That(spots.GroupBy(s => s.MapNumber).Select(g => g.Count()), Is.All.EqualTo(2));
        foreach (var characterClass in this._gameConfiguration.CharacterClasses)
        {
            foreach (var attribute in MUnique.OpenMU.GameLogic.PvP.PvpStats.All)
            {
                Assert.That(characterClass.StatAttributes.Any(a => a.Attribute == attribute), Is.True, $"{characterClass.Name}: {attribute.Designation}");
            }
        }
    }

    /// <summary>
    /// Tests that there is only one free Devil Square and Blood Castle room for all levels, with double experience,
    /// and that the events and the jewels for hunting are active.
    /// </summary>
    [Test]
    public void EventsAreConfigured()
    {
        foreach (var (type, level) in new[] { (MiniGameType.DevilSquare, ClassicEvents.DevilSquareLevel), (MiniGameType.BloodCastle, ClassicEvents.BloodCastleLevel) })
        {
            var active = this._gameConfiguration.MiniGameDefinitions.Where(d => d.Type == type && d.IsActive).ToList();
            Assert.That(active.Select(d => d.GameLevel), Is.EqualTo(new[] { level }), type.ToString());
            var definition = active.Single();
            Assert.That(definition.TicketItem, Is.Null);
            Assert.That(definition.FreeTicketItem, Is.Null);
            Assert.That(definition.MinimumCharacterLevel, Is.EqualTo(10));
            Assert.That(definition.MaximumCharacterLevel, Is.EqualTo(400));
            Assert.That(definition.MapCreationPolicy, Is.EqualTo(MiniGameMapCreationPolicy.Shared));
            Assert.That(definition.Entrance!.Map!.ExpMultiplier, Is.EqualTo(2));
        }

        foreach (var plugInType in new[] { typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.BloodCastleStartPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents.GoldenInvasionPlugIn), typeof(MUnique.OpenMU.GameLogic.HuntingRewards.HuntingJewelPlugIn) })
        {
            Assert.That(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == plugInType.GUID).IsActive, Is.True, plugInType.Name);
        }

        var devilSquare = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartPlugIn).GUID);
        Assert.That(devilSquare.CustomConfiguration, Does.Contain("21:00"));
    }

    /// <summary>
    /// Lists the active content with an unknown version, which is not restricted. Informational only.
    /// </summary>
    [Test]
    [Explicit]
    public void ListActiveContentWithUnknownVersion()
    {
        foreach (var map in this._gameConfiguration.Maps.Where(m => m.IsActive && m.IntroducedIn == GameVersion.Unknown))
        {
            TestContext.Out.WriteLine($"Map {map.Number}: {map.Name}");
        }

        foreach (var item in this._gameConfiguration.Items.Where(m => m.IsActive && m.IntroducedIn == GameVersion.Unknown).OrderBy(i => i.Group).ThenBy(i => i.Number))
        {
            TestContext.Out.WriteLine($"Item {item.Group}/{item.Number}: {item.Name}");
        }

        foreach (var monster in this._gameConfiguration.Monsters.Where(m => m.IsActive && m.IntroducedIn == GameVersion.Unknown).OrderBy(m => m.Number))
        {
            TestContext.Out.WriteLine($"Monster {monster.Number}: {monster.Designation}");
        }

        foreach (var miniGame in this._gameConfiguration.MiniGameDefinitions.Where(m => m.IsActive && m.IntroducedIn == GameVersion.Unknown))
        {
            TestContext.Out.WriteLine($"MiniGame {miniGame.Type} {miniGame.GameLevel}");
        }
    }
}
