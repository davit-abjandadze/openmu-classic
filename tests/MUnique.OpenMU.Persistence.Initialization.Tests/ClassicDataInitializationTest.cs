// <copyright file="ClassicDataInitializationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.Persistence.Initialization.Updates;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests the classic (Season 1 content with DK, DW and Elf) data initialization for the Season 6 client.
/// </summary>
[TestFixture]
internal class ClassicDataInitializationTest
{
    private GameConfiguration _gameConfiguration = null!;
    private IContext _context = null!;
    private InMemoryPersistenceContextProvider _contextProvider = null!;

    /// <summary>
    /// Creates the classic data once for all tests.
    /// </summary>
    [OneTimeSetUp]
    public async Task SetUpAsync()
    {
        this._contextProvider = new InMemoryPersistenceContextProvider();
        var dataInitialization = new ClassicDataInitialization(this._contextProvider, new NullLoggerFactory());
        await dataInitialization.CreateInitialDataAsync(1, true).ConfigureAwait(false);
        this._context = this._contextProvider.CreateNewContext();
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
    /// Tests that the warp list offers no warps to removed or newer maps (Aida, Kanturu, Elvenland, Karutan, ...),
    /// while it still offers the classic ones.
    /// </summary>
    [Test]
    public void WarpListOffersOnlyActiveMaps()
    {
        var warps = MUnique.OpenMU.GameLogic.PlugIns.ShowWarpListAfterEnterWorldPlugIn.GetSelectableWarps(this._gameConfiguration.WarpList);
        var warpIndexes = warps.Select(w => (int)w.Index).ToList();

        Assert.That(this._gameConfiguration.Maps.Single(m => m.Number == 33).IsActive, Is.False, "Aida");
        Assert.That(warps.Select(w => w.Gate!.Map!).Where(m => !m.IsActive), Is.Empty);
        Assert.That(warpIndexes, Does.Contain(2).And.Contain(23), "Lorencia and Icarus");
        Assert.That(warpIndexes, Has.None.AnyOf(25, 27, 28, 29, 30, 31, 32, 33, 34, 42, 43, 45, 46, 47, 48));
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
    /// Tests that skills and the scrolls and orbs which teach them have no level requirement, while other requirements stay.
    /// </summary>
    [Test]
    public void SkillsHaveNoLevelRequirement()
    {
        static bool IsLevel(AttributeRequirement r) => r.Attribute?.Id == GameLogic.Attributes.Stats.Level.Id;

        Assert.That(this._gameConfiguration.Skills.Where(s => s.Requirements.Any(IsLevel)).Select(s => s.Number), Is.Empty);
        var learningItems = this._gameConfiguration.Items.Where(i => i.Skill is not null && i.Group is 12 or 15).ToList();
        Assert.That(learningItems, Is.Not.Empty);
        Assert.That(learningItems.Where(i => i.Requirements.Any(IsLevel)).Select(i => (i.Group, i.Number)), Is.Empty);
        var scrollOfPoison = this._gameConfiguration.Items.Single(i => i.Group == 15 && i.Number == 0);
        Assert.That(scrollOfPoison.Requirements, Is.Not.Empty, "the energy requirement stays");
    }

    /// <summary>
    /// Tests that the update removes the skill level requirements from an existing classic database.
    /// </summary>
    [Test]
    public async Task UpdateRemovesSkillLevelRequirementsAsync()
    {
        var level = this._gameConfiguration.Attributes.First(a => a.Id == GameLogic.Attributes.Stats.Level.Id);
        var skill = this._gameConfiguration.Skills.First(s => s.Number == 41); // Twisting Slash
        var requirement = this._context.CreateNew<AttributeRequirement>();
        requirement.Attribute = level;
        requirement.MinimumValue = 80;
        skill.Requirements.Add(requirement);

        await new ClassicSkillLevelRequirementsUpdatePlugIn().ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(skill.Requirements.Any(r => r.Attribute?.Id == level.Id), Is.False);
    }

    /// <summary>
    /// Tests that the buffs of Dark Wizard, Dark Knight and Fairy Elf can only be cast on the caster itself.
    /// </summary>
    /// <param name="skillNumber">The skill number.</param>
    [TestCase(16)] // Soul Barrier
    [TestCase(27)] // Greater Defense
    [TestCase(28)] // Greater Damage
    [TestCase(48)] // Swell Life
    public void BuffsAreSelfOnly(short skillNumber)
    {
        var skill = this._gameConfiguration.Skills.Single(s => s.Number == skillNumber);

        Assert.That(skill.TargetRestriction, Is.EqualTo(SkillTargetRestriction.Self));
        Assert.That(skill.Target, Is.Not.EqualTo(SkillTarget.ImplicitParty));
    }

    /// <summary>
    /// Tests that the update restricts the buffs to the caster in an existing classic database.
    /// </summary>
    [Test]
    public async Task UpdateRestrictsBuffsToSelfAsync()
    {
        var greaterDamage = this._gameConfiguration.Skills.Single(s => s.Number == 28);
        var swellLife = this._gameConfiguration.Skills.Single(s => s.Number == 48);
        greaterDamage.TargetRestriction = SkillTargetRestriction.Player;
        swellLife.TargetRestriction = SkillTargetRestriction.Undefined;
        swellLife.Target = SkillTarget.ImplicitParty;

        await new ClassicSelfOnlyBuffsUpdatePlugIn().ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(greaterDamage.TargetRestriction, Is.EqualTo(SkillTargetRestriction.Self));
        Assert.That(swellLife.TargetRestriction, Is.EqualTo(SkillTargetRestriction.Self));
        Assert.That(swellLife.Target, Is.EqualTo(SkillTarget.ImplicitPlayer));
    }

    /// <summary>
    /// Tests that no store sells the potion stacks of three pieces anymore, and that every store sells
    /// stacks of 255 large healing and large mana potions, which don't overlap other items.
    /// </summary>
    [Test]
    public void AllStoresSellLargePotionStacks()
    {
        var stores = this._gameConfiguration.Monsters.Select(m => m.MerchantStore).OfType<ItemStorage>().ToList();

        Assert.That(stores, Is.Not.Empty);
        foreach (var store in stores)
        {
            Assert.That(store.Items.Where(i => i.Definition!.Group == 14 && (int)i.Durability == 3), Is.Empty);
            foreach (var potionNumber in new short[] { 3, 6 })
            {
                Assert.That(
                    store.Items.Count(i => i.Definition!.Group == 14 && i.Definition.Number == potionNumber && (int)i.Durability == ClassicMerchantStores.LargePotionStackSize),
                    Is.EqualTo(1));
            }

            static IEnumerable<int> Cells(Item item) => Enumerable.Range(0, item.Definition!.Height)
                .SelectMany(r => Enumerable.Range(0, item.Definition.Width).Select(c => item.ItemSlot + (r * 8) + c));
            var largeStacks = store.Items.Where(i => (int)i.Durability == ClassicMerchantStores.LargePotionStackSize).ToList();
            var otherCells = store.Items.Except(largeStacks).SelectMany(Cells).ToList();
            Assert.That(largeStacks.SelectMany(Cells), Is.Unique);
            Assert.That(largeStacks.SelectMany(Cells).Intersect(otherCells), Is.Empty);
        }
    }

    /// <summary>
    /// Tests that every active skill scroll and orb is sold by the store of its class, and only there:
    /// Dark Wizard at Pasi the Mage, Dark Knight at Hanzo the Blacksmith and Fairy Elf at Elf Lala.
    /// </summary>
    /// <param name="merchantNumber">The number of the merchant.</param>
    /// <param name="firstClassNumber">The number of the first class of the class family.</param>
    [TestCase(254, 0)]
    [TestCase(251, 4)]
    [TestCase(242, 8)]
    public void SkillStoreSellsAllSkillItemsOfItsClass(short merchantNumber, byte firstClassNumber)
    {
        var classes = Enumerable.Range(firstClassNumber, 4).Select(n => (byte)n).ToHashSet();
        bool IsSkillItem(ItemDefinition? definition) => definition?.Skill is not null && definition.Group is 12 or 15;
        bool IsForClass(ItemDefinition definition) => definition.QualifiedCharacters.Any(c => classes.Contains(c.Number));

        var store = this._gameConfiguration.Monsters.Single(m => m.Number == merchantNumber).MerchantStore!;
        var expected = this._gameConfiguration.Items
            .Where(i => i.IsActive && IsSkillItem(i) && IsForClass(i))
            .SelectMany(i => Enumerable.Range(0, i.MaximumItemLevel + 1).Select(level => (i.Group, i.Number, level)))
            .ToList();
        var sold = store.Items
            .Where(i => IsSkillItem(i.Definition))
            .Select(i => (i.Definition!.Group, i.Definition.Number, level: (int)i.Level))
            .ToList();

        Assert.That(expected, Is.Not.Empty);
        Assert.That(sold, Is.EquivalentTo(expected));

        static IEnumerable<int> Cells(Item item) => Enumerable.Range(0, item.Definition!.Height)
            .SelectMany(r => Enumerable.Range(0, item.Definition.Width).Select(c => item.ItemSlot + (r * 8) + c));
        Assert.That(store.Items.SelectMany(Cells), Is.Unique);
        Assert.That(store.Items.SelectMany(Cells), Is.All.LessThan(120));
    }

    /// <summary>
    /// Tests that the update replaces the potion stacks of three pieces of an existing classic database,
    /// and that applying it again doesn't add the large potion stacks twice.
    /// </summary>
    [Test]
    public async Task UpdateReplacesThreePieceStacksAsync()
    {
        var store = this._gameConfiguration.Monsters.First(m => m.MerchantStore is not null).MerchantStore!;
        var largeHealing = this._gameConfiguration.Items.Single(i => i.Group == 14 && i.Number == 3);
        foreach (var large in store.Items.Where(i => i.Definition == largeHealing).ToList())
        {
            store.Items.Remove(large);
        }

        var stack = this._context.CreateNew<Item>();
        stack.Definition = largeHealing;
        stack.Durability = 3;
        stack.ItemSlot = 119;
        store.Items.Add(stack);

        var update = new ClassicLargePotionStacksUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(store.Items, Does.Not.Contain(stack));
        Assert.That(store.Items.Count(i => i.Definition == largeHealing && (int)i.Durability == 255), Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that the new database has the Season 6 updates installed, because the classic data is built on the
    /// Season 6 data and the update service treats it as a Season 6 database by its client version.
    /// Otherwise, the next start would apply them all again and e.g. reactivate the Chaos Castle.
    /// </summary>
    [Test]
    public async Task NoMandatoryUpdateIsPendingAsync()
    {
        var plugInManager = new PlugInManager(null, new NullLoggerFactory(), null, null);
        plugInManager.DiscoverAndRegisterPlugIns(typeof(DataUpdateService).Assembly);
        var updateService = new DataUpdateService(this._contextProvider, plugInManager);

        var installed = await updateService.GetInstalledUpdatesAsync().ConfigureAwait(false);
        var pending = await updateService.DetermineMandatoryUpdatesAsync().ConfigureAwait(false);

        Assert.That(installed, Is.Not.Empty);
        Assert.That(pending.Select(update => update.Name), Is.Empty);
    }

    /// <summary>
    /// Tests that the test accounts only have characters of the classic classes (Dark Wizard, Dark Knight and Fairy Elf,
    /// with their second classes), and none of the classes which the classic server doesn't have, like the Dark Lord,
    /// the Summoner, the Rage Fighter, the Magic Gladiator or the master classes. A master class character would
    /// open the master skill tree in the client.
    /// </summary>
    [Test]
    public async Task TestAccountsHaveOnlyClassicCharactersAsync()
    {
        int[] classicClasses = [0, 2, 4, 6, 8, 10];
        var characters = (await this._context.GetAsync<Account>().ConfigureAwait(false)).SelectMany(a => a.Characters).ToList();

        Assert.That(characters, Is.Not.Empty);
        Assert.That(characters.Select(c => (int)c.CharacterClass!.Number).Distinct(), Is.SubsetOf(classicClasses));
        Assert.That(characters.Where(c => c.CharacterClass!.IsMasterClass).Select(c => c.Name), Is.Empty);
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
    /// Tests that the cash shop is deactivated.
    /// </summary>
    [Test]
    public void CashShopIsDeactivated()
    {
        var cashShop = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.CashShop.CashShopFeaturePlugIn).GUID);
        Assert.That(cashShop.IsActive, Is.False);
    }

    /// <summary>
    /// Tests that all inventory extensions are unlocked from the start.
    /// </summary>
    [Test]
    public void StartingInventoryExtensionsAreActive()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.StartingInventoryExtensionsPlugIn).GUID);
        Assert.That(plugIn.IsActive, Is.True);
        Assert.That(plugIn.CustomConfiguration?.Replace(" ", string.Empty), Does.Contain("\"Extensions\":4"));
    }

    /// <summary>
    /// Tests that the update activates the starting inventory extensions on an existing classic database
    /// which doesn't have the plugin configuration yet, and that applying it again doesn't add a second one.
    /// </summary>
    [Test]
    public async Task UpdateActivatesStartingInventoryExtensionsAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.PlugIns.StartingInventoryExtensionsPlugIn).GUID;
        this._gameConfiguration.PlugInConfigurations.Remove(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId));

        var update = new ClassicStartingInventoryExtensionsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        Assert.That(plugIn.IsActive, Is.True);
    }

    /// <summary>
    /// Tests that the item level limit is active with +11, while the upgrade mixes up to +13 stay, so the limit can be raised.
    /// </summary>
    [Test]
    public void ItemLevelLimitIsActiveWithLevel11()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.ItemLevelLimitPlugIn).GUID);
        Assert.That(plugIn.IsActive, Is.True);
        Assert.That(plugIn.CustomConfiguration?.Replace(" ", string.Empty), Does.Contain("\"MaximumItemLevel\":11"));
    }

    /// <summary>
    /// Tests that the update activates the item level limit on an existing classic database
    /// which doesn't have the plugin configuration yet, and that applying it again doesn't add a second one.
    /// </summary>
    [Test]
    public async Task UpdateActivatesItemLevelLimitAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.PlugIns.ItemLevelLimitPlugIn).GUID;
        this._gameConfiguration.PlugInConfigurations.Remove(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId));

        var update = new ClassicItemLevelLimitUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        Assert.That(plugIn.IsActive, Is.True);
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

        foreach (var plugInType in new[] { typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.BloodCastleStartPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents.GoldenInvasionPlugIn), typeof(MUnique.OpenMU.GameLogic.HuntingRewards.HuntingJewelPlugIn), typeof(MUnique.OpenMU.GameLogic.MiniGames.Medals.MiniGameMedalsPlugIn), typeof(MUnique.OpenMU.GameLogic.HuntingRewards.HuntingPointsPlugIn), typeof(MUnique.OpenMU.GameLogic.ItemExchange.ItemExchangeChatCommandPlugIn), typeof(MUnique.OpenMU.GameLogic.TeamVsTeam.TeamVsTeamPlugIn) })
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

    /// <summary>
    /// Tests that the Box of Heaven (Box of Luck +7) always drops a normal +7 to +11 weapon or armor piece,
    /// out of all active equipment which drops from monsters, and never zen.
    /// </summary>
    [Test]
    public void BoxOfHeavenDropsNormalEquipment()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var group = box.DropItems.Single(g => g.SourceItemLevel == ClassicBoxes.BoxOfHeavenLevel);

        Assert.That(group.ItemType, Is.EqualTo(SpecialItemType.RandomItem));
        Assert.That(group.Chance, Is.EqualTo(1.0));
        Assert.That(group.MinimumLevel, Is.EqualTo(7));
        Assert.That(group.MaximumLevel, Is.EqualTo(11));
        Assert.That(group.PossibleItems, Is.EquivalentTo(this._gameConfiguration.Items.Where(ClassicBoxes.IsBoxOfHeavenItem)));
        Assert.That(group.PossibleItems, Has.All.Matches<ItemDefinition>(i => i.Group <= 11 && i.IsActive && !i.IsAmmunition));
        Assert.That(group.PossibleItems.Select(i => (i.Group, i.Number)), Does.Contain(((byte)0, (short)0)).And.Contain(((byte)8, (short)0)).And.Contain(((byte)6, (short)0)));
    }

    /// <summary>
    /// Tests that monsters of level 30 and above drop the Box of Heaven on all maps with the common jewel drop.
    /// </summary>
    [Test]
    public void MonstersDropBoxOfHeaven()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var dropGroup = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == ClassicBoxes.GetBoxOfHeavenMonsterDropId());

        Assert.That(dropGroup.PossibleItems, Is.EquivalentTo(new[] { box }));
        Assert.That(dropGroup.ItemLevel, Is.EqualTo(ClassicBoxes.BoxOfHeavenLevel));
        Assert.That(dropGroup.Chance, Is.EqualTo(0.001));
        Assert.That(dropGroup.MinimumMonsterLevel, Is.EqualTo(30));

        var mapsWithJewels = this._gameConfiguration.Maps.Where(m => m.DropItemGroups.Any(g => g.ItemType == SpecialItemType.Jewel)).ToList();
        Assert.That(mapsWithJewels.Select(m => (int)m.Number), Does.Contain(0).And.Contain(2).And.Contain(7));
        Assert.That(mapsWithJewels, Has.All.Matches<GameMapDefinition>(m => m.DropItemGroups.Contains(dropGroup)));
    }

    /// <summary>
    /// Tests that Caren the Barmaid sells the Box of Kundun +1 to +5, without overlapping other items.
    /// </summary>
    [Test]
    public void CarenSellsBoxesOfKundun()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var store = this._gameConfiguration.Monsters.Single(m => m.Number == 244).MerchantStore!;

        Assert.That(store.Items.Where(i => i.Definition == box).Select(i => (int)i.Level), Is.EquivalentTo(new[] { 8, 9, 10, 11, 12 }));

        static IEnumerable<int> Cells(Item item) => Enumerable.Range(0, item.Definition!.Height)
            .SelectMany(r => Enumerable.Range(0, item.Definition.Width).Select(c => item.ItemSlot + (r * 8) + c));
        Assert.That(store.Items.SelectMany(Cells), Is.Unique);
    }

    /// <summary>
    /// Tests that the update changes the Box of Heaven and the store of Caren the Barmaid of an existing classic database,
    /// which still has the jewel and zen drops of the Box of Heaven, and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateChangesBoxOfHeavenAndCarensStoreAsync()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var group = box.DropItems.Single(g => g.SourceItemLevel == ClassicBoxes.BoxOfHeavenLevel);
        group.PossibleItems.Clear();
        group.PossibleItems.Add(this._gameConfiguration.Items.Single(i => i.Group == 14 && i.Number == 13));
        group.Chance = 0.5;
        group.MinimumLevel = 0;
        group.MaximumLevel = 0;
        var zen = this._context.CreateNew<ItemDropItemGroup>();
        zen.ItemType = SpecialItemType.Money;
        zen.MoneyAmount = 1000;
        zen.SourceItemLevel = ClassicBoxes.BoxOfHeavenLevel;
        zen.Chance = 1.0;
        box.DropItems.Add(zen);

        var store = this._gameConfiguration.Monsters.Single(m => m.Number == 244).MerchantStore!;
        foreach (var boxItem in store.Items.Where(i => i.Definition == box).ToList())
        {
            store.Items.Remove(boxItem);
        }

        var dropGroup = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == ClassicBoxes.GetBoxOfHeavenMonsterDropId());
        this._gameConfiguration.DropItemGroups.Remove(dropGroup);
        foreach (var map in this._gameConfiguration.Maps)
        {
            map.DropItemGroups.Remove(dropGroup);
        }

        var update = new ClassicBoxesUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(box.DropItems.Where(g => g.SourceItemLevel == ClassicBoxes.BoxOfHeavenLevel), Is.EquivalentTo(new[] { group }));
        Assert.That(group.Chance, Is.EqualTo(1.0));
        Assert.That(group.MinimumLevel, Is.EqualTo(7));
        Assert.That(group.MaximumLevel, Is.EqualTo(11));
        Assert.That(group.PossibleItems, Is.EquivalentTo(this._gameConfiguration.Items.Where(ClassicBoxes.IsBoxOfHeavenItem)));
        Assert.That(store.Items.Where(i => i.Definition == box).Select(i => (int)i.Level), Is.EquivalentTo(new[] { 8, 9, 10, 11, 12 }));

        var newDropGroup = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == ClassicBoxes.GetBoxOfHeavenMonsterDropId());
        var lorencia = this._gameConfiguration.Maps.First(m => m.Number == 0);
        Assert.That(lorencia.DropItemGroups.Count(g => g == newDropGroup), Is.EqualTo(1));
    }
}
