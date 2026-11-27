// <copyright file="ClassicDataInitializationTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;
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
    /// Checks the fresh monster assignments and repairs an existing classic configuration twice.
    /// </summary>
    [Test]
    public async Task AtlansMonstersUseNormalAttacksAsync()
    {
        var lizard = this._gameConfiguration.Monsters.Single(m => m.Number == 48);
        var valkyrie = this._gameConfiguration.Monsters.Single(m => m.Number == 52);
        Assert.That(lizard.AttackSkill, Is.Null);
        Assert.That(valkyrie.AttackSkill, Is.Null);
        var others = this._gameConfiguration.Monsters.Where(m => m.Number is not (48 or 52)).ToList();
        var otherSkills = others.Select(m => m.AttackSkill).ToList();
        lizard.AttackSkill = this._gameConfiguration.Skills.Single(s => s.Number == (short)Skills.SkillNumber.Lightning);
        valkyrie.AttackSkill = this._gameConfiguration.Skills.Single(s => s.Number == (short)Skills.SkillNumber.Ice);
        var update = new ClassicAtlansAttacksUpdatePlugIn();
        for (var run = 0; run < 2; run++)
        {
            await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
            Assert.That(lizard.AttackSkill, Is.Null);
            Assert.That(valkyrie.AttackSkill, Is.Null);
            Assert.That(others.Select(m => m.AttackSkill), Is.EqualTo(otherSkills));
        }
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
    /// Tests that only Dark Wizard, Dark Knight, Fairy Elf and Magic Gladiator can be created, and only they and their second classes are active.
    /// </summary>
    [Test]
    public void OnlyClassicClassesAreActive()
    {
        var active = this._gameConfiguration.CharacterClasses.Where(c => c.IsActive).Select(c => (int)c.Number).Order();
        var creatable = this._gameConfiguration.CharacterClasses.Where(c => c.CanGetCreated).Select(c => (int)c.Number).Order();

        Assert.That(active, Is.EqualTo(new[] { 0, 2, 4, 6, 8, 10, 12 }));
        Assert.That(creatable, Is.EqualTo(new[] { 0, 4, 8, 12 }));
    }

    /// <summary>
    /// Tests that every account can create a Magic Gladiator from the start, by the active <see cref="UnlockedCharacterClassesPlugIn"/>.
    /// </summary>
    [Test]
    public void MagicGladiatorIsUnlockedForEveryAccount()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(p => p.TypeId == typeof(UnlockedCharacterClassesPlugIn).GUID);

        Assert.That(plugIn.IsActive, Is.True);
        Assert.That(plugIn.GetConfiguration<UnlockedCharacterClassesConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())?.ClassNumbers, Is.EquivalentTo(new byte[] { 12 }));
    }

    /// <summary>
    /// Tests that the update activates the Magic Gladiator, its items and the <see cref="UnlockedCharacterClassesPlugIn"/>
    /// in an existing classic database where it's inactive, without activating non-classic items.
    /// </summary>
    [Test]
    public async Task UpdateActivatesMagicGladiatorAsync()
    {
        var magicGladiator = this._gameConfiguration.CharacterClasses.Single(c => c.Number == 12);
        var magicGladiatorItems = this._gameConfiguration.Items
            .Where(i => i.IsActive && i.QualifiedCharacters.Count > 0 && i.QualifiedCharacters.All(c => c.Number == 12))
            .ToList();
        var activeItems = this._gameConfiguration.Items.Count(i => i.IsActive);
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(p => p.TypeId == typeof(UnlockedCharacterClassesPlugIn).GUID);
        magicGladiator.IsActive = false;
        magicGladiator.CanGetCreated = false;
        magicGladiatorItems.ForEach(i => i.IsActive = false);
        plugIn.IsActive = false;

        await new ClassicMagicGladiatorUpdatePlugIn().ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(magicGladiator.IsActive, Is.True);
        Assert.That(magicGladiator.CanGetCreated, Is.True);
        Assert.That(magicGladiatorItems.Select(i => i.IsActive), Is.All.True);
        Assert.That(this._gameConfiguration.Items.Count(i => i.IsActive), Is.EqualTo(activeItems));
        Assert.That(plugIn.IsActive, Is.True);
    }

    /// <summary>
    /// Tests that no content newer than Season 1 is active, except the bosses which the classic server spawns additionally
    /// and the suppliers of the Castle Siege.
    /// </summary>
    [Test]
    public void NoNewerContentIsActive()
    {
        var target = ClassicContentRestriction.TargetVersion;
        Assert.That(this._gameConfiguration.Maps.Where(m => m.IsActive && m.IntroducedIn > target), Is.Empty);
        var addedMonsters = ClassicBosses.Bosses.Where(b => b.Spawn is not null).Select(b => b.MonsterNumber).Concat(ClassicCastleSiege.SupplierNumbers).ToHashSet();
        Assert.That(this._gameConfiguration.Monsters.Where(m => m.IsActive && m.IntroducedIn > target && !addedMonsters.Contains(m.Number)), Is.Empty);
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
    /// Checks the client energy thresholds on fresh data and after upgrading the old skill table.
    /// </summary>
    [Test]
    public async Task SummonEnergyMatchesOrbLevelsAsync()
    {
        var energy = this._gameConfiguration.Skills.Where(s => s.Number is >= 30 and <= 36)
            .OrderBy(s => s.Number)
            .Select(s => s.Requirements.Single(r => r.Attribute?.Id == Stats.TotalEnergy.Id)).ToList();
        int[] expected = [30, 60, 90, 130, 170, 210, 300];
        Assert.That(energy.Select(r => r.MinimumValue), Is.EqualTo(expected));
        energy[3].MinimumValue = 230;
        var update = new ClassicSkillLevelRequirementsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        Assert.That(energy.Select(r => r.MinimumValue), Is.EqualTo(expected));
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
    /// Tests that no store sells the potion stacks of three pieces anymore, and that every store except the medal merchant
    /// sells stacks of 255 large healing and large mana potions, which don't overlap other items.
    /// </summary>
    [Test]
    public void AllStoresSellLargePotionStacks()
    {
        var stores = this._gameConfiguration.Monsters
            .Where(m => m.Number != MedalMerchantConfiguration.DefaultMerchantNumber)
            .Select(m => m.MerchantStore)
            .OfType<ItemStorage>()
            .ToList();

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
    /// Tests that the scrolls of Decay, Ice Storm and Nova are inactive, so they don't drop, and that Pasi the Mage doesn't sell them.
    /// </summary>
    [Test]
    public void DecayIceStormAndNovaScrollsAreRemoved()
    {
        var scrolls = this._gameConfiguration.Items.Where(i => i.Group == 15 && i.Number is 16 or 17 or 18).ToList();
        var pasi = this._gameConfiguration.Monsters.Single(m => m.Number == 254).MerchantStore!;

        Assert.That(scrolls, Has.Count.EqualTo(3));
        Assert.That(scrolls.Select(i => i.IsActive), Is.All.False);
        Assert.That(pasi.Items.Where(i => scrolls.Contains(i.Definition!)), Is.Empty);
    }

    /// <summary>
    /// Tests that the update deactivates the scrolls of Decay, Ice Storm and Nova of an existing classic database
    /// and removes them from the store of Pasi the Mage.
    /// </summary>
    [Test]
    public async Task UpdateRemovesDecayIceStormAndNovaScrollsAsync()
    {
        var scrolls = this._gameConfiguration.Items.Where(i => i.Group == 15 && i.Number is 16 or 17 or 18).ToList();
        var pasi = this._gameConfiguration.Monsters.Single(m => m.Number == 254).MerchantStore!;
        scrolls.ForEach(i => i.IsActive = true);
        await ClassicMerchantStores.ApplyAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        Assume.That(pasi.Items.Where(i => scrolls.Contains(i.Definition!)), Is.Not.Empty);

        var update = new ClassicRemovedSkillScrollsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(scrolls.Select(i => i.IsActive), Is.All.False);
        Assert.That(pasi.Items.Where(i => scrolls.Contains(i.Definition!)), Is.Empty);
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
    /// Tests that the test accounts only have characters of the classic classes (Dark Wizard, Dark Knight, Fairy Elf and Magic Gladiator,
    /// with their second classes), and none of the classes which the classic server doesn't have, like the Dark Lord,
    /// the Summoner, the Rage Fighter or the master classes. A master class character would
    /// open the master skill tree in the client.
    /// </summary>
    [Test]
    public async Task TestAccountsHaveOnlyClassicCharactersAsync()
    {
        int[] classicClasses = [0, 2, 4, 6, 8, 10, 12];
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
        Assert.That(experience.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetExperienceRateConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())?.GetMultiplier(0), Is.EqualTo(1));
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
    /// Tests that at most 2 accounts can be logged in from one IP address.
    /// </summary>
    [Test]
    public void MaximumAccountsPerIpIsActiveWithTwo()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugIn).GUID);
        Assert.That(plugIn.IsActive, Is.True);
        var configuration = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugInConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        Assert.That(configuration?.MaximumConnectionsPerIp, Is.EqualTo(2));
    }

    /// <summary>
    /// Tests that the update activates the limit of 2 accounts per IP address on an existing classic database,
    /// whose plugin configuration is still inactive with the former default of 3, and that applying it again doesn't add a second one.
    /// </summary>
    [Test]
    public async Task UpdateActivatesMaximumAccountsPerIpAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugIn).GUID;
        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        var existing = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        existing.IsActive = false;
        existing.SetConfiguration(new MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugInConfiguration { MaximumConnectionsPerIp = 3 }, referenceHandler);

        var update = new ClassicMaximumAccountsPerIpUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        Assert.That(plugIn.IsActive, Is.True);
        Assert.That(plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugInConfiguration>(referenceHandler)?.MaximumConnectionsPerIp, Is.EqualTo(2));
    }

    /// <summary>
    /// Tests that the update creates the plugin configuration of the account limit per IP address, when the database doesn't have it.
    /// </summary>
    [Test]
    public async Task UpdateCreatesMaximumAccountsPerIpAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.PlugIns.MaximumConnectionsPerIpPlugIn).GUID;
        this._gameConfiguration.PlugInConfigurations.Remove(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId));

        await new ClassicMaximumAccountsPerIpUpdatePlugIn().ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId).IsActive, Is.True);
    }

    /// <summary>
    /// Tests that only one account of an IP address can take part in the same event.
    /// </summary>
    [Test]
    public void EventAccountsPerIpIsActiveWithOne()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.MiniGames.EventAccountsPerIpPlugIn).GUID);
        Assert.That(plugIn.IsActive, Is.True);
        var configuration = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.MiniGames.EventAccountsPerIpConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        Assert.That(configuration?.MaximumAccountsPerIp, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that the update activates the limit of one account per IP address in events on an existing classic database,
    /// whose plugin configuration is still inactive, and that applying it again doesn't add a second one.
    /// </summary>
    [Test]
    public async Task UpdateActivatesEventAccountsPerIpAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.MiniGames.EventAccountsPerIpPlugIn).GUID;
        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        var existing = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        existing.IsActive = false;
        existing.SetConfiguration(new MUnique.OpenMU.GameLogic.MiniGames.EventAccountsPerIpConfiguration { MaximumAccountsPerIp = 3 }, referenceHandler);

        var update = new ClassicEventAccountsPerIpUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        Assert.That(plugIn.IsActive, Is.True);
        Assert.That(plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.MiniGames.EventAccountsPerIpConfiguration>(referenceHandler)?.MaximumAccountsPerIp, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that the update creates the plugin configuration of the account limit per IP address in events, when the database doesn't have it.
    /// </summary>
    [Test]
    public async Task UpdateCreatesEventAccountsPerIpAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.MiniGames.EventAccountsPerIpPlugIn).GUID;
        this._gameConfiguration.PlugInConfigurations.Remove(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId));

        await new ClassicEventAccountsPerIpUpdatePlugIn().ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId).IsActive, Is.True);
    }

    /// <summary>
    /// Tests that the monsters of Tarkan and Icarus drop excellent items between the Legendary, Dragon and Guardian sets (50)
    /// and below the Grand Soul and Dark Phoenix sets (69), and that these limits are really the borders of the named sets.
    /// </summary>
    [Test]
    public void ExcellentDropLevelsOfTarkanAndIcarusAreActive()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.MapExcellentDropLevelsPlugIn).GUID);
        Assert.That(plugIn.IsActive, Is.True);
        var configuration = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.PlugIns.MapExcellentDropLevelsConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())!;
        Assert.That(
            configuration.Maps.Select(m => (m.MapNumber, m.MinimumDropLevel, m.MaximumDropLevel)),
            Is.EquivalentTo(new (short, int, int)[] { (8, 50, 69), (10, 50, 69) }));

        int DropLevel(byte group, short number) => this._gameConfiguration.Items.Single(i => i.Group == group && i.Number == number).DropLevel;
        Assert.That(DropLevel(7, 3), Is.EqualTo(ClassicPlugInDefaults.HighMapsMinimumExcellentDropLevel), "Legendary Helm");
        Assert.That(DropLevel(10, 14), Is.EqualTo(ClassicPlugInDefaults.HighMapsMinimumExcellentDropLevel), "Guardian Gloves");
        Assert.That(DropLevel(10, 1), Is.GreaterThan(ClassicPlugInDefaults.HighMapsMinimumExcellentDropLevel), "Dragon Gloves");
        Assert.That(DropLevel(8, 9), Is.LessThan(ClassicPlugInDefaults.HighMapsMinimumExcellentDropLevel), "Plate Armor");
        Assert.That(DropLevel(10, 18), Is.EqualTo(ClassicPlugInDefaults.HighMapsMaximumExcellentDropLevel + 1), "Grand Soul Gloves");
        Assert.That(DropLevel(10, 17), Is.GreaterThan(ClassicPlugInDefaults.HighMapsMaximumExcellentDropLevel), "Dark Phoenix Gloves");
        Assert.That(DropLevel(8, 1), Is.LessThanOrEqualTo(ClassicPlugInDefaults.HighMapsMaximumExcellentDropLevel), "Dragon Armor");
    }

    /// <summary>
    /// Tests that the update activates the excellent item drop levels of Tarkan and Icarus on an existing classic database
    /// which doesn't have the plugin configuration yet, and that applying it again doesn't add a second one.
    /// </summary>
    [Test]
    public async Task UpdateActivatesExcellentDropLevelsAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.PlugIns.MapExcellentDropLevelsPlugIn).GUID;
        this._gameConfiguration.PlugInConfigurations.Remove(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId));

        var update = new ClassicExcellentDropLevelsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId);
        Assert.That(plugIn.IsActive, Is.True);
        var configuration = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.PlugIns.MapExcellentDropLevelsConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())!;
        Assert.That(configuration.Maps, Has.Count.EqualTo(2));
    }

    /// <summary>
    /// Tests that the Castle Siege participants receive 5 Boxes of Kundun +4 and that monsters drop the Sign of Lord (Rena +3).
    /// </summary>
    [Test]
    public void CastleSiegeRewardsAndSignOfLordDropAreConfigured()
    {
        this.AssertCastleSiegeRewardsAndSignOfLordDrop();
    }

    /// <summary>
    /// Tests that the update configures the Castle Siege rewards and the Sign of Lord drop on an existing classic database
    /// which doesn't have them yet, and that applying it again doesn't add a second drop group.
    /// </summary>
    [Test]
    public async Task UpdateConfiguresCastleSiegeRewardsAndSignOfLordDropAsync()
    {
        var castleSiege = this._gameConfiguration.CastleSiegeConfiguration!;
        castleSiege.RewardItemDefinition = null;
        castleSiege.RewardItemLevel = 0;
        castleSiege.RewardItemCount = 0;
        castleSiege.ParticipantRewardMinSeconds = 60;
        var dropGroup = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == ClassicCastleSiege.GetSignOfLordDropId());
        this._gameConfiguration.DropItemGroups.Remove(dropGroup);
        foreach (var map in this._gameConfiguration.Maps)
        {
            map.DropItemGroups.Remove(dropGroup);
        }

        var update = new ClassicCastleSiegeRewardsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        this.AssertCastleSiegeRewardsAndSignOfLordDrop();
    }

    /// <summary>
    /// Tests that all plugins of the Castle Siege are active, including the internal packet handlers of the game server
    /// like the group handler of the Castle Siege packets (C1 B2), and that the suppliers of the Valley of Loren are active.
    /// </summary>
    [Test]
    public void CastleSiegePlugInsAndSuppliersAreActive()
    {
        this.AssertCastleSiegePlugInsAndSuppliersAreActive();
    }

    /// <summary>
    /// Tests that the update activates the plugins of the Castle Siege and the suppliers on an existing classic database
    /// where they are inactive, and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateActivatesCastleSiegePlugInsAndSuppliersAsync()
    {
        var typeIds = ClassicPlugInDefaults.GetCastleSiegePlugInTypes().Select(t => t.GUID).ToHashSet();
        foreach (var plugIn in this._gameConfiguration.PlugInConfigurations.Where(c => typeIds.Contains(c.TypeId)))
        {
            plugIn.IsActive = false;
        }

        foreach (var supplier in this._gameConfiguration.Monsters.Where(m => ClassicCastleSiege.SupplierNumbers.Contains(m.Number)))
        {
            supplier.IsActive = false;
        }

        var update = new ClassicCastleSiegePlugInsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        this.AssertCastleSiegePlugInsAndSuppliersAreActive();
    }

    /// <summary>
    /// Tests that every party member gains the experience of a solo kill.
    /// </summary>
    [Test]
    public void PartySoloExperienceIsActive()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.PartySoloExperiencePlugIn).GUID);
        Assert.That(plugIn.IsActive, Is.True);
    }

    /// <summary>
    /// Tests that the update activates the solo experience in a party on an existing classic database
    /// which doesn't have the plugin configuration yet, and that applying it again doesn't add a second one.
    /// </summary>
    [Test]
    public async Task UpdateActivatesPartySoloExperienceAsync()
    {
        var typeId = typeof(MUnique.OpenMU.GameLogic.PlugIns.PartySoloExperiencePlugIn).GUID;
        this._gameConfiguration.PlugInConfigurations.Remove(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId));

        var update = new ClassicPartySoloExperienceUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeId).IsActive, Is.True);
    }

    /// <summary>
    /// Tests that the update replaces the former absolute experience rates by resets (540 at zero resets)
    /// by the relative ones, and that it keeps a configuration which was changed by hand.
    /// </summary>
    [Test]
    public async Task UpdateReplacesAbsoluteResetExperienceRatesAsync()
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.Resets.ResetExperienceRatePlugIn).GUID);
        var absolute = new MUnique.OpenMU.GameLogic.Resets.ResetExperienceRateConfiguration();
        absolute.Tiers.Add(new() { MinimumResetCount = 0, Multiplier = 540 });
        absolute.Tiers.Add(new() { MinimumResetCount = 5, Multiplier = 432 });
        plugIn.SetConfiguration(absolute, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());

        var update = new ClassicRelativeResetExperienceRatesUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        var configuration = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetExperienceRateConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())!;
        Assert.That(configuration.GetMultiplier(0), Is.EqualTo(1));
        Assert.That(configuration.GetMultiplier(5), Is.EqualTo(0.8f));

        var custom = new MUnique.OpenMU.GameLogic.Resets.ResetExperienceRateConfiguration();
        custom.Tiers.Add(new() { MinimumResetCount = 0, Multiplier = 3 });
        plugIn.SetConfiguration(custom, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        Assert.That(plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetExperienceRateConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())!.GetMultiplier(0), Is.EqualTo(3));
    }

    /// <summary>
    /// Tests that the classic reset configuration gives 500 points per reset count: 500 after the first reset,
    /// 1000 after the second and 1500 after the third one.
    /// </summary>
    /// <param name="currentResetCount">The reset count before the reset.</param>
    /// <param name="expectedPoints">The expected points after the reset.</param>
    [TestCase(0, 500)]
    [TestCase(1, 1000)]
    [TestCase(2, 1500)]
    [TestCase(54, 27500)]
    public void ResetPointsRiseBy500PerReset(int currentResetCount, int expectedPoints)
    {
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.Resets.ResetFeaturePlugIn).GUID);
        var configuration = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())!;

        var progression = MUnique.OpenMU.GameLogic.Resets.ResetProgressionCalculator.Calculate(currentResetCount, 0, configuration);

        Assert.That(configuration.ReplacePointsPerReset, Is.True);
        Assert.That(progression.TotalPointsAfterReset, Is.EqualTo(expectedPoints));
    }

    /// <summary>
    /// Tests that the update replaces the former tiered reset points (2000, 1500, 1000) by 500 points per reset count,
    /// keeps the other reset settings, and keeps points which were changed by hand.
    /// </summary>
    [Test]
    public async Task UpdateReplacesTieredResetPointsAsync()
    {
        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.Resets.ResetFeaturePlugIn).GUID);
        var former = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetConfiguration>(referenceHandler)!;
        former.MultiplyPointsByResetCount = false;
        former.PointsPerReset = 1000;
        former.RequiredLevel = 360;
        former.PointsTiers.Add(new() { MinimumResetCount = 1, PointsGranted = 2000 });
        former.PointsTiers.Add(new() { MinimumResetCount = 2, PointsGranted = 1500 });
        former.PointsTiers.Add(new() { MinimumResetCount = 5, PointsGranted = 1000 });
        plugIn.SetConfiguration(former, referenceHandler);

        var update = new ClassicResetPointsPerResetCountUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        var updated = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetConfiguration>(referenceHandler)!;
        Assert.That(updated.PointsTiers, Is.Empty);
        Assert.That(updated.RequiredLevel, Is.EqualTo(360));
        Assert.That(MUnique.OpenMU.GameLogic.Resets.ResetProgressionCalculator.Calculate(2, 0, updated).TotalPointsAfterReset, Is.EqualTo(1500));

        var custom = plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetConfiguration>(referenceHandler)!;
        custom.PointsTiers.Add(new() { MinimumResetCount = 1, PointsGranted = 700 });
        plugIn.SetConfiguration(custom, referenceHandler);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        Assert.That(plugIn.GetConfiguration<MUnique.OpenMU.GameLogic.Resets.ResetConfiguration>(referenceHandler)!.PointsTiers, Has.Count.EqualTo(1));
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
    /// Tests that the Devil Square and the Blood Castle have free tiers (six and seven), which are entered in instances per party,
    /// with double experience, and that the events are active, but not the hunting rewards.
    /// </summary>
    [Test]
    public void EventsAreConfigured()
    {
        AssertInstancedEvents(this._gameConfiguration);

        foreach (var plugInType in new[] { typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.BloodCastleStartPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents.GoldenInvasionPlugIn), typeof(MUnique.OpenMU.GameLogic.MiniGames.Medals.MiniGameMedalsPlugIn), typeof(MUnique.OpenMU.GameLogic.ItemExchange.ItemExchangeChatCommandPlugIn), typeof(MUnique.OpenMU.GameLogic.TeamVsTeam.TeamVsTeamPlugIn) })
        {
            Assert.That(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == plugInType.GUID).IsActive, Is.True, plugInType.Name);
        }

        foreach (var plugInType in new[] { typeof(MUnique.OpenMU.GameLogic.HuntingRewards.HuntingJewelPlugIn), typeof(MUnique.OpenMU.GameLogic.HuntingRewards.HuntingPointsPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.ChaosCastleStartPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents.RedDragonInvasionPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents.WhiteWizardInvasionPlugIn), typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.HappyHourPlugIn) })
        {
            Assert.That(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == plugInType.GUID).IsActive, Is.False, plugInType.Name);
        }

    }

    /// <summary>
    /// Tests that the active tiers of the Devil Square and the Blood Castle have active monsters to fight.
    /// </summary>
    [Test]
    public void EveryEventTierHasActiveMonsters()
    {
        foreach (var definition in this._gameConfiguration.MiniGameDefinitions.Where(d => d.Type is MiniGameType.DevilSquare or MiniGameType.BloodCastle && d.IsActive))
        {
            var map = definition.Entrance!.Map!;
            var monsters = map.MonsterSpawns
                .Where(s => s.SpawnTrigger is SpawnTrigger.OnceAtEventStart or SpawnTrigger.AutomaticDuringEvent or SpawnTrigger.AutomaticDuringWave or SpawnTrigger.OnceAtWaveStart)
                .Where(s => s.MonsterDefinition is { IsActive: true, ObjectKind: NpcObjectKind.Monster })
                .ToList();
            Assert.That(map.IsActive, Is.True, definition.Name.ToString());
            Assert.That(monsters, Is.Not.Empty, definition.Name.ToString());
        }
    }

    /// <summary>
    /// Tests that the update replaces the former nightly events on an existing classic database,
    /// and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateConfiguresInstancedEventsAsync()
    {
        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        foreach (var definition in this._gameConfiguration.MiniGameDefinitions.Where(d => d.Type is MiniGameType.DevilSquare or MiniGameType.BloodCastle))
        {
            // The state of the former nightly events: one shared room for all levels.
            definition.IsActive = definition.GameLevel == 4;
            definition.MapCreationPolicy = MiniGameMapCreationPolicy.Shared;
            definition.MaximumPlayerCount = 10;
            definition.MinimumCharacterLevel = 10;
            definition.MaximumCharacterLevel = 400;
        }

        var devilSquare = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartPlugIn).GUID);
        var formerConfiguration = MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartConfiguration.Default;
        formerConfiguration.Timetable = [new TimeOnly(21, 0)];
        devilSquare.SetConfiguration(formerConfiguration, referenceHandler);
        var characterClass = this._gameConfiguration.CharacterClasses.First();
        foreach (var statAttribute in characterClass.StatAttributes.Where(a => MUnique.OpenMU.GameLogic.MiniGames.MiniGameEntryStats.All.Contains(a.Attribute)).ToList())
        {
            characterClass.StatAttributes.Remove(statAttribute);
        }

        var update = new ClassicInstancedEventsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        AssertInstancedEvents(this._gameConfiguration);
        foreach (var attribute in MUnique.OpenMU.GameLogic.MiniGames.MiniGameEntryStats.All)
        {
            Assert.That(this._gameConfiguration.Attributes.Count(a => a == attribute), Is.EqualTo(1), attribute.Designation);
        }
    }

    private static void AssertInstancedEvents(GameConfiguration gameConfiguration)
    {
        foreach (var (type, tierLevels) in new[] { (MiniGameType.DevilSquare, ClassicEvents.DevilSquareTierLevels), (MiniGameType.BloodCastle, ClassicEvents.BloodCastleTierLevels) })
        {
            var active = gameConfiguration.MiniGameDefinitions.Where(d => d.Type == type && d.IsActive).OrderBy(d => d.GameLevel).ToList();
            Assert.That(active.Select(d => (int)d.GameLevel), Is.EqualTo(Enumerable.Range(1, tierLevels.Count)), type.ToString());
            Assert.That(tierLevels[0], Is.EqualTo((10, 60)));
            Assert.That(tierLevels[^1].Maximum, Is.EqualTo(400));
            foreach (var definition in active)
            {
                var (minimumLevel, maximumLevel) = tierLevels[definition.GameLevel - 1];
                Assert.That(definition.TicketItem, Is.Null);
                Assert.That(definition.FreeTicketItem, Is.Null);
                Assert.That(definition.RequiresMasterClass, Is.False);
                Assert.That(definition.MinimumCharacterLevel, Is.EqualTo(minimumLevel));
                Assert.That(definition.MaximumCharacterLevel, Is.EqualTo(maximumLevel));
                Assert.That(definition.MinimumPlayerCount, Is.EqualTo(1));
                Assert.That(definition.MaximumPlayerCount, Is.EqualTo(ClassicEvents.MaximumPlayersPerRoom));
                Assert.That(definition.MapCreationPolicy, Is.EqualTo(MiniGameMapCreationPolicy.OnePerParty));
                Assert.That(definition.Entrance!.Map!.ExpMultiplier, Is.EqualTo(2));
            }
        }

        Assert.That(ClassicEvents.BloodCastleTierLevels, Has.Count.EqualTo(7));
        Assert.That(ClassicEvents.DevilSquareTierLevels, Has.Count.EqualTo(6));

        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        var devilSquare = gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartPlugIn).GUID)
            .GetConfiguration<MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartConfiguration>(referenceHandler)!;
        var bloodCastle = gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.BloodCastleStartPlugIn).GUID)
            .GetConfiguration<MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.BloodCastleStartConfiguration>(referenceHandler)!;
        // The localized strings are only deserialized with the converter which the server registers, so the raw configuration is checked.
        Assert.That(gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.DevilSquareStartPlugIn).GUID).CustomConfiguration, Does.Contain("Devil Square opens in {0} minute(s)! Join with /joinds"));
        Assert.That(gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.BloodCastleStartPlugIn).GUID).CustomConfiguration, Does.Contain("Blood Castle opens in {0} minute(s)! Join with /joinbc"));
        Assert.That(devilSquare.Timetable, Is.EqualTo(new[] { new TimeOnly(13, 0), new TimeOnly(19, 0), new TimeOnly(22, 0) }));
        Assert.That(bloodCastle.Timetable, Is.EqualTo(new[] { new TimeOnly(13, 30), new TimeOnly(19, 30), new TimeOnly(22, 30) }));
        foreach (var configuration in new MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks.InstancedMiniGameStartConfiguration[] { devilSquare, bloodCastle })
        {
            Assert.That(configuration.IsSharedPerTier, Is.True);
            Assert.That(configuration.EntranceDuration, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(configuration.GameDuration, Is.EqualTo(TimeSpan.FromMinutes(15)));
            Assert.That(configuration.AnnouncementLeadTimes, Is.EqualTo(new[] { TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2) }));
            Assert.That(configuration.PartySizeScalings.Select(p => p.MonsterCountFactor), Is.EqualTo(new[] { 0.6, 0.8, 1.0, 1.15, 1.3 }));
            Assert.That(configuration.PartySizeScalings.Select(p => p.MonsterHealthFactor), Is.EqualTo(new[] { 0.7, 0.85, 1.0, 1.1, 1.2 }));
            Assert.That(configuration.PartySizeScalings.Select(p => p.RewardFactor), Is.EqualTo(new[] { 1.5, 1.2, 1.0, 0.9, 0.85 }));
        }

        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            foreach (var attribute in MUnique.OpenMU.GameLogic.MiniGames.MiniGameEntryStats.All)
            {
                Assert.That(characterClass.StatAttributes.Count(a => a.Attribute == attribute), Is.EqualTo(1), $"{characterClass.Name}: {attribute.Designation}");
            }
        }
    }

    /// <summary>
    /// Tests that the classic data gets the rates which the server runs with: experience, money and the drop chances.
    /// </summary>
    [Test]
    public void RatesAreConfigured()
    {
        Assert.That(this._gameConfiguration.ExperienceRate, Is.EqualTo(ClassicRates.ExperienceRate));
        Assert.That(this._gameConfiguration.ItemDropDuration, Is.EqualTo(TimeSpan.FromSeconds(50)));
        Assert.That(this._gameConfiguration.GlobalBaseAttributeValues.Single(a => a.Definition?.Id == Stats.MoneyAmountRate.Id).Value, Is.EqualTo(500));

        double ChanceOf(Guid id) => this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == id).Chance;
        Assert.That(ChanceOf(GuidHelper.CreateGuid<DropItemGroup>(4)), Is.EqualTo(0.02), "jewels");
        Assert.That(ChanceOf(GuidHelper.CreateGuid<DropItemGroup>(3)), Is.EqualTo(0.01), "excellent items");
        Assert.That(ChanceOf(GuidHelper.CreateGuid<DropItemGroup>(10, 1)), Is.EqualTo(0.05), "Loch's Feather");
        Assert.That(ChanceOf(GuidHelper.CreateGuid<DropItemGroup>(0, 23, 4)), Is.EqualTo(0.5), "Scroll of Emperor (Dark Knight)");
        Assert.That(ChanceOf(GuidHelper.CreateGuid<DropItemGroup>(1, 26, 0)), Is.EqualTo(0.5), "Soul Shard of Wizard");
        Assert.That(ChanceOf(GuidHelper.CreateGuid<DropItemGroup>(14, 11, 7)), Is.EqualTo(0.01), "Box of Heaven");
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
    /// up to the tier of Dragon Armor, and never zen.
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
        Assert.That(group.PossibleItems, Has.All.Matches<ItemDefinition>(i => i.Group <= 11 && i.IsActive && !i.IsAmmunition && i.DropLevel <= 59));
        Assert.That(group.PossibleItems.Select(i => (i.Group, i.Number)), Does.Contain(((byte)0, (short)0)).And.Contain(((byte)8, (short)0)).And.Contain(((byte)6, (short)0)));
    }

    /// <summary>
    /// Verifies that version 4 removes high-grade rewards without restoring manually excluded items.
    /// </summary>
    [Test]
    public async Task UpdateCapsBoxOfHeavenRewardsAsync()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var group = box.DropItems.Single(g => g.SourceItemLevel == ClassicBoxes.BoxOfHeavenLevel);
        var low = group.PossibleItems.First(i => i.DropLevel < 59);
        group.PossibleItems.Remove(low);
        group.PossibleItems.Add(this._gameConfiguration.Items.Single(i => i.Group == 8 && i.Number == 16));
        var update = new ClassicBoxesUpdatePlugIn();
        await this.SetBoxUpdateVersionAsync(update, 3).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(group.PossibleItems.Any(i => i.DropLevel > 59), Is.False);
        Assert.That(group.PossibleItems, Does.Not.Contain(low));
        Assert.That(group.PossibleItems.Any(i => i.Group == 8 && i.Number == 1), Is.True);
        group.PossibleItems.Add(low);
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
        Assert.That(dropGroup.Chance, Is.EqualTo(ClassicBoxes.BoxOfHeavenDropChance));
        Assert.That(dropGroup.MinimumMonsterLevel, Is.EqualTo(30));

        var mapsWithJewels = this._gameConfiguration.Maps.Where(m => m.DropItemGroups.Any(g => g.ItemType == SpecialItemType.Jewel)).ToList();
        Assert.That(mapsWithJewels.Select(m => (int)m.Number), Does.Contain(0).And.Contain(2).And.Contain(7));
        Assert.That(mapsWithJewels, Has.All.Matches<GameMapDefinition>(m => m.DropItemGroups.Contains(dropGroup)));
    }

    /// <summary>
    /// Tests that the rings and pendants don't drop randomly like other equipment, but with their own rarer drop groups
    /// on all maps with the common jewel drop, and that the Box of Kundun +2 and +3 still drop them as excellent items.
    /// </summary>
    [Test]
    public void RingsAndPendantsDropRarely()
    {
        this.AssertJewelryDrops();

        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var jewelry = this._gameConfiguration.Items.Where(i => ClassicJewelryDrops.Jewelry.Contains((i.Group, i.Number))).ToList();
        var fromBoxes = ClassicBoxes.GetBoxOfKundunItemGroup(box, 9)!.PossibleItems
            .Concat(ClassicBoxes.GetBoxOfKundunItemGroup(box, 10)!.PossibleItems);
        Assert.That(fromBoxes.Intersect(jewelry), Is.EquivalentTo(jewelry));
    }

    /// <summary>
    /// Tests that the update makes the rings and pendants rarer on an existing classic database, where they still drop
    /// randomly and the jewelry drop groups are missing, and that applying it again doesn't add anything twice.
    /// </summary>
    [Test]
    public async Task UpdateMakesRingsAndPendantsRarerAsync()
    {
        var jewelry = this._gameConfiguration.Items.Where(i => ClassicJewelryDrops.Jewelry.Contains((i.Group, i.Number))).ToList();
        foreach (var item in jewelry)
        {
            item.DropsFromMonsters = true;
        }

        foreach (var id in new[] { ClassicJewelryDrops.GetNormalDropGroupId(), ClassicJewelryDrops.GetExcellentDropGroupId() })
        {
            var dropGroup = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == id);
            this._gameConfiguration.DropItemGroups.Remove(dropGroup);
            foreach (var map in this._gameConfiguration.Maps)
            {
                map.DropItemGroups.Remove(dropGroup);
            }
        }

        var update = new ClassicJewelryDropsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        this.AssertJewelryDrops();
        var lorencia = this._gameConfiguration.Maps.First(m => m.Number == 0);
        Assert.That(lorencia.DropItemGroups.Count(g => g.GetId() == ClassicJewelryDrops.GetNormalDropGroupId()), Is.EqualTo(1));
        Assert.That(lorencia.DropItemGroups.Count(g => g.GetId() == ClassicJewelryDrops.GetExcellentDropGroupId()), Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that Caren the Barmaid sells the Box of Kundun +1 to +4, without overlapping other items.
    /// </summary>
    [Test]
    public void CarenSellsBoxesOfKundun()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var store = this._gameConfiguration.Monsters.Single(m => m.Number == 244).MerchantStore!;

        Assert.That(store.Items.Where(i => i.Definition == box).Select(i => (int)i.Level), Is.EquivalentTo(new[] { 8, 9, 10, 11 }));

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
        await this.SetBoxUpdateVersionAsync(update, 0).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(box.DropItems.Where(g => g.SourceItemLevel == ClassicBoxes.BoxOfHeavenLevel), Is.EquivalentTo(new[] { group }));
        Assert.That(group.Chance, Is.EqualTo(1.0));
        Assert.That(group.MinimumLevel, Is.EqualTo(7));
        Assert.That(group.MaximumLevel, Is.EqualTo(11));
        Assert.That(group.PossibleItems, Is.EquivalentTo(this._gameConfiguration.Items.Where(ClassicBoxes.IsBoxOfHeavenItem)));
        Assert.That(store.Items.Where(i => i.Definition == box).Select(i => (int)i.Level), Is.EquivalentTo(new[] { 8, 9, 10, 11 }));

        var newDropGroup = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == ClassicBoxes.GetBoxOfHeavenMonsterDropId());
        var lorencia = this._gameConfiguration.Maps.First(m => m.Number == 0);
        Assert.That(lorencia.DropItemGroups.Count(g => g == newDropGroup), Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that the Box of Kundun +5 is held back: no merchant sells it and no monster drops it.
    /// </summary>
    [Test]
    public void BoxOfKundunPlus5IsHeldBack()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;

        Assert.That(this._gameConfiguration.DropItemGroups.Where(g => g.ItemLevel == ClassicBoxes.LastBoxOfKundunLevel && g.PossibleItems.Contains(box)), Is.Empty);
        Assert.That(this._gameConfiguration.Monsters.Where(m => m.MerchantStore is not null).SelectMany(m => m.MerchantStore!.Items).Where(i => i.Definition == box && i.Level == ClassicBoxes.LastBoxOfKundunLevel), Is.Empty);
        Assert.That(this._gameConfiguration.Monsters.Single(m => m.Number == 82).DropItemGroups.Select(g => (int)g.ItemLevel!.Value), Is.EqualTo(new[] { (int)ClassicBoxes.HighestBoxOfKundunLevel }), "Golden Tantalos");
    }

    /// <summary>
    /// Tests that the update holds the Box of Kundun +5 back in an existing classic database, in which Caren the Barmaid
    /// sells it and the Golden Tantalos drops it, and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateHoldsBoxOfKundunPlus5BackAsync()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var store = this._gameConfiguration.Monsters.Single(m => m.Number == 244).MerchantStore!;
        var plus5 = this._context.CreateNew<Item>();
        plus5.Definition = box;
        plus5.Level = ClassicBoxes.LastBoxOfKundunLevel;
        plus5.Durability = 1;
        plus5.ItemSlot = 119;
        store.Items.Add(plus5);
        var tantalosDrop = this._gameConfiguration.Monsters.Single(m => m.Number == 82).DropItemGroups.Single();
        tantalosDrop.ItemLevel = ClassicBoxes.LastBoxOfKundunLevel;
        var dropGroupCount = this._gameConfiguration.DropItemGroups.Count;

        var update = new ClassicBoxesUpdatePlugIn();
        await this.SetBoxUpdateVersionAsync(update, 2).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(store.Items.Where(i => i.Definition == box).Select(i => (int)i.Level), Is.EquivalentTo(new[] { 8, 9, 10, 11 }));
        Assert.That(tantalosDrop.ItemLevel, Is.EqualTo(ClassicBoxes.HighestBoxOfKundunLevel));
        Assert.That(this._gameConfiguration.DropItemGroups, Has.Count.EqualTo(dropGroupCount));
    }

    /// <summary>
    /// Tests that the excellent Archangel weapons drop from the Box of Kundun +5 and not from the +4.
    /// </summary>
    [Test]
    public void ArchangelWeaponsDropFromBoxOfKundunPlus5()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var plus4 = ClassicBoxes.GetBoxOfKundunItemGroup(box, ClassicBoxes.BoxOfKundunPlus4Level)!;
        var plus5 = ClassicBoxes.GetBoxOfKundunItemGroup(box, ClassicBoxes.LastBoxOfKundunLevel)!;
        var weapons = this._gameConfiguration.Items.Where(i => ClassicBoxes.ArchangelWeapons.Contains((i.Group, i.Number))).ToList();

        Assert.That(weapons, Has.Count.EqualTo(ClassicBoxes.ArchangelWeapons.Count));
        Assert.That(plus5.PossibleItems, Is.SupersetOf(weapons));
        Assert.That(plus4.PossibleItems.Intersect(weapons), Is.Empty);
        Assert.That(plus4.PossibleItems.Select(i => (i.Group, i.Number)), Does.Contain(((byte)0, (short)16)), "Sword of Destruction stays in the +4");
    }

    /// <summary>
    /// Tests that the update moves the Archangel weapons of an existing classic database from the Box of Kundun +4
    /// to the +5, and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateMovesArchangelWeaponsToBoxOfKundunPlus5Async()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var plus4 = ClassicBoxes.GetBoxOfKundunItemGroup(box, ClassicBoxes.BoxOfKundunPlus4Level)!;
        var plus5 = ClassicBoxes.GetBoxOfKundunItemGroup(box, ClassicBoxes.LastBoxOfKundunLevel)!;
        var weapons = this._gameConfiguration.Items.Where(i => ClassicBoxes.ArchangelWeapons.Contains((i.Group, i.Number))).ToList();
        foreach (var weapon in weapons)
        {
            plus5.PossibleItems.Remove(weapon);
            plus4.PossibleItems.Add(weapon);
        }

        var plus4Count = plus4.PossibleItems.Count;
        var plus5Count = plus5.PossibleItems.Count;

        var update = new ClassicBoxesUpdatePlugIn();
        await this.SetBoxUpdateVersionAsync(update, 1).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(plus4.PossibleItems.Intersect(weapons), Is.Empty);
        Assert.That(plus5.PossibleItems, Is.SupersetOf(weapons));
        Assert.That(plus4.PossibleItems, Has.Count.EqualTo(plus4Count - weapons.Count));
        Assert.That(plus5.PossibleItems, Has.Count.EqualTo(plus5Count + weapons.Count));
    }

    /// <summary>
    /// Tests that the boss list update removes the configuration of the former "/bosses" chat command plugin,
    /// and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateRemovesFormerBossStatusChatCommandAsync()
    {
        var removed = this._context.CreateNew<PlugInConfiguration>();
        removed.TypeId = ClassicBossListsUpdatePlugIn.RemovedBossStatusChatCommandPlugInId;
        removed.IsActive = true;
        this._gameConfiguration.PlugInConfigurations.Add(removed);
        var count = this._gameConfiguration.PlugInConfigurations.Count;

        var update = new ClassicBossListsUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(this._gameConfiguration.PlugInConfigurations, Does.Not.Contain(removed));
        Assert.That(this._gameConfiguration.PlugInConfigurations, Has.Count.EqualTo(count - 1));
    }

    /// <summary>
    /// Tests that the bosses respawn after their delays and drop a Box of Kundun and silver medals on their map only.
    /// </summary>
    [Test]
    public void BossesDropBoxesOfKundunAndSilverMedals()
    {
        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        var expected = new (short Monster, short Map, int BoxLevel, int Medals, int Minutes)[]
        {
            (49, 7, 10, 1, 70), // Hydra, Atlans, Box of Kundun +3
            (38, 4, 10, 1, 70), // Balrog, Lost Tower, Box of Kundun +3
            (59, 8, 11, 1, 120), // Zaikan, Tarkan, Box of Kundun +4
            (63, 8, 11, 1, 120), // Death Beam Knight, Tarkan, Box of Kundun +4
            (77, 10, 11, 6, 240), // Dark Phoenix, Icarus, Box of Kundun +4
            (309, 6, 11, 6, 240), // Hell Maine, Arena, Box of Kundun +4
        };

        foreach (var (monsterNumber, mapNumber, boxLevel, medals, minutes) in expected)
        {
            var monster = this._gameConfiguration.Monsters.Single(m => m.Number == monsterNumber);
            var map = this._gameConfiguration.Maps.Single(m => m.Number == mapNumber);
            var groups = map.DropItemGroups.Where(g => g.Monster == monster).ToList();

            Assert.That(monster.RespawnDelay, Is.EqualTo(TimeSpan.FromMinutes(minutes)), monster.Designation.ToString());
            Assert.That(groups.All(g => g.Chance >= 1.0 && g.PossibleItems.SequenceEqual(new[] { box })), Is.True);
            Assert.That(groups.Select(g => (int)g.ItemLevel!.Value), Is.EquivalentTo(new[] { boxLevel }.Concat(Enumerable.Repeat(5, medals))));
            Assert.That(monster.NumberOfMaximumItemDrops, Is.GreaterThanOrEqualTo(groups.Count + ClassicBosses.NormalDrops));
            Assert.That(this._gameConfiguration.Maps.Where(m => m != map).SelectMany(m => m.DropItemGroups).Where(g => g.Monster == monster), Is.Empty);
        }
    }

    /// <summary>
    /// Tests that Hell Maine is active and spawns once on a walkable tile outside of the safezone of the Arena.
    /// </summary>
    [Test]
    public void HellMaineSpawnsInArena()
    {
        var hellMaine = this._gameConfiguration.Monsters.Single(m => m.Number == 309);
        var arena = this._gameConfiguration.Maps.Single(m => m.Number == 6);
        var spawn = arena.MonsterSpawns.Single(s => s.MonsterDefinition == hellMaine);
        var terrain = new GameMapTerrain(arena);

        Assert.That(hellMaine.IsActive, Is.True);
        Assert.That(spawn.Quantity, Is.EqualTo(1));
        Assert.That(spawn.SpawnTrigger, Is.EqualTo(SpawnTrigger.Automatic));
        Assert.That(terrain.WalkMap[spawn.X1, spawn.Y1], Is.True);
        Assert.That(terrain.SafezoneMap[spawn.X1, spawn.Y1], Is.False);
    }

    /// <summary>
    /// Tests that the update adds Hell Maine and its spawn in the Arena to an existing database, also when it's applied twice.
    /// </summary>
    [Test]
    public async Task HellMaineUpdateIsRepeatableAsync()
    {
        var hellMaine = this._gameConfiguration.Monsters.Single(m => m.Number == 309);
        var arena = this._gameConfiguration.Maps.Single(m => m.Number == 6);
        foreach (var spawn in arena.MonsterSpawns.Where(s => s.MonsterDefinition == hellMaine).ToList())
        {
            arena.MonsterSpawns.Remove(spawn);
        }

        hellMaine.IsActive = false;

        var update = new ClassicBossesUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(hellMaine.IsActive, Is.True);
        Assert.That(arena.MonsterSpawns.Count(s => s.MonsterDefinition == hellMaine), Is.EqualTo(1));
        Assert.That(arena.DropItemGroups.Count(g => g.Monster == hellMaine), Is.EqualTo(7));
    }

    /// <summary>
    /// Tests that the update adds the boss drops to an existing database, also when it's applied twice.
    /// </summary>
    [Test]
    public async Task BossesUpdateIsRepeatableAsync()
    {
        var phoenix = this._gameConfiguration.Monsters.Single(m => m.Number == 77);
        var icarus = this._gameConfiguration.Maps.Single(m => m.Number == 10);
        foreach (var group in icarus.DropItemGroups.Where(g => g.Monster == phoenix).ToList())
        {
            icarus.DropItemGroups.Remove(group);
            this._gameConfiguration.DropItemGroups.Remove(group);
        }

        phoenix.RespawnDelay = TimeSpan.FromSeconds(10);

        var update = new ClassicBossesUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(phoenix.RespawnDelay, Is.EqualTo(TimeSpan.FromMinutes(240)));
        Assert.That(icarus.DropItemGroups.Count(g => g.Monster == phoenix), Is.EqualTo(7));
        var boss = ClassicBosses.Bosses.Single(b => b.MonsterNumber == 77);
        var ids = Enumerable.Range(0, 7).Select(i => ClassicBosses.GetDropGroupId(boss, (byte)i)).ToList();
        Assert.That(this._gameConfiguration.DropItemGroups.Count(g => ids.Contains(g.GetId())), Is.EqualTo(7));
    }

    /// <summary>
    /// Tests that the bosses are much stronger than normal monsters, and that the strength update sets
    /// the values again, also when it's applied twice, without duplicating the attributes.
    /// </summary>
    [Test]
    public async Task BossesAreStrongAsync()
    {
        foreach (var boss in ClassicBosses.Bosses)
        {
            var monster = this._gameConfiguration.Monsters.Single(m => m.Number == boss.MonsterNumber);
            Assert.That(GetValue(monster, Stats.MaximumHealth), Is.EqualTo(boss.Strength.Health), boss.Name);
            Assert.That(GetValue(monster, Stats.DefenseBase), Is.EqualTo(boss.Strength.Defense), boss.Name);
            Assert.That(boss.Strength.Health, Is.GreaterThanOrEqualTo(500_000), boss.Name);
        }

        var phoenix = this._gameConfiguration.Monsters.Single(m => m.Number == 77);
        phoenix.Attributes.Single(a => a.AttributeDefinition == Stats.MaximumHealth.GetPersistent(this._gameConfiguration)).Value = 95_000;
        var attributeCount = phoenix.Attributes.Count;

        var update = new ClassicBossStrengthUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(GetValue(phoenix, Stats.MaximumHealth), Is.EqualTo(5_000_000));
        Assert.That(GetValue(phoenix, Stats.MinimumPhysBaseDmg), Is.LessThan(GetValue(phoenix, Stats.MaximumPhysBaseDmg)));
        Assert.That(phoenix.Attributes.Count, Is.EqualTo(attributeCount));

        float GetValue(MonsterDefinition monster, AttributeSystem.AttributeDefinition attribute)
            => monster.Attributes.Single(a => a.AttributeDefinition == attribute.GetPersistent(this._gameConfiguration)).Value;
    }

    /// <summary>
    /// Tests that Phantom Knight, Drakan, Alpha Crust and Great Drakan have four times the original damage and twice the
    /// original health and defense, and that the update
    /// halves the health and defense of an existing database, also when it's applied twice, without duplicating the attributes.
    /// </summary>
    [Test]
    public async Task IcarusMonstersAreStrongerAsync()
    {
        var greatDrakan = this._gameConfiguration.Monsters.Single(m => m.Number == 75);
        Assert.That(GetValue(greatDrakan, Stats.MaximumHealth), Is.EqualTo(50_000 * ClassicIcarusMonsters.HealthAndDefenseFactor));
        Assert.That(GetValue(greatDrakan, Stats.DefenseBase), Is.EqualTo(495 * ClassicIcarusMonsters.HealthAndDefenseFactor));
        Assert.That(GetValue(greatDrakan, Stats.Level), Is.EqualTo(100));
        foreach (var (number, name, strength) in ClassicIcarusMonsters.Monsters)
        {
            var monster = this._gameConfiguration.Monsters.Single(m => m.Number == number);
            Assert.That(GetValue(monster, Stats.MaximumHealth), Is.EqualTo(strength.Health), name);
            Assert.That(GetValue(monster, Stats.MaximumPhysBaseDmg), Is.EqualTo(strength.MaximumDamage), name);
            Assert.That(GetValue(monster, Stats.DefenseRatePvm), Is.EqualTo(strength.DefenseRate), name);
        }

        var drakan = this._gameConfiguration.Monsters.Single(m => m.Number == 73);
        drakan.Attributes.Single(a => a.AttributeDefinition == Stats.MaximumHealth.GetPersistent(this._gameConfiguration)).Value = 116_000;
        drakan.Attributes.Single(a => a.AttributeDefinition == Stats.DefenseBase.GetPersistent(this._gameConfiguration)).Value = 1_220;
        var attributeCount = drakan.Attributes.Count;

        var update = new ClassicIcarusMonsterHealthUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(GetValue(drakan, Stats.MaximumHealth), Is.EqualTo(58_000));
        Assert.That(GetValue(drakan, Stats.DefenseBase), Is.EqualTo(610));
        Assert.That(GetValue(drakan, Stats.MaximumPhysBaseDmg), Is.EqualTo(1_920));
        Assert.That(drakan.Attributes.Count, Is.EqualTo(attributeCount));

        float GetValue(MonsterDefinition monster, AttributeSystem.AttributeDefinition attribute)
            => monster.Attributes.Single(a => a.AttributeDefinition == attribute.GetPersistent(this._gameConfiguration)).Value;
    }

    /// <summary>
    /// Tests that Thompson the Merchant stands on a walkable safezone tile next to the fountain of Lorencia,
    /// sells only the Box of Kundun +3 and +4, and that the medal merchant plugin is active with their prices.
    /// </summary>
    [Test]
    public void MedalMerchantSellsBoxesOfKundun()
    {
        var merchant = this._gameConfiguration.Monsters.Single(m => m.Number == MedalMerchantConfiguration.DefaultMerchantNumber);
        var lorencia = this._gameConfiguration.Maps.Single(m => m.Number == 0);
        var spawn = lorencia.MonsterSpawns.Single(s => s.MonsterDefinition == merchant);
        var terrain = new GameMapTerrain(lorencia);

        Assert.That(merchant.IsActive, Is.True);
        Assert.That(merchant.NpcWindow, Is.EqualTo(NpcWindow.Merchant));
        Assert.That((spawn.X1, spawn.Y1), Is.EqualTo((ClassicMedalMerchant.X, ClassicMedalMerchant.Y)));
        Assert.That(terrain.WalkMap[spawn.X1, spawn.Y1] && terrain.SafezoneMap[spawn.X1, spawn.Y1], Is.True);
        Assert.That(lorencia.MonsterSpawns.Where(s => s != spawn).Any(s => s.X1 == spawn.X1 && s.Y1 == spawn.Y1), Is.False);
        Assert.That(this._gameConfiguration.Maps.SelectMany(m => m.MonsterSpawns).Count(s => s.MonsterDefinition == merchant), Is.EqualTo(1));

        var box = ClassicBoxes.GetBoxOfLuck(this._gameConfiguration)!;
        Assert.That(merchant.MerchantStore!.Items.All(i => i.Definition == box), Is.True);
        Assert.That(merchant.MerchantStore.Items.Select(i => (int)i.Level).Order(), Is.EqualTo(new[] { 10, 11 }));

        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MedalMerchantPlugIn).GUID);
        var configuration = plugIn.GetConfiguration<MedalMerchantConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        Assert.That(plugIn.IsActive, Is.True);
        Assert.That(configuration!.MerchantNumber, Is.EqualTo(merchant.Number));
        Assert.That(merchant.MerchantStore.Items.Select(i => configuration.GetPrice(i)?.SilverMedals).Order(), Is.EqualTo(new int?[] { 5, 10 }));
    }

    /// <summary>
    /// Tests that the medal merchant update replaces the potions of the store by the boxes and adds the spawn
    /// to an existing database, also when it's applied twice.
    /// </summary>
    [Test]
    public async Task MedalMerchantUpdateIsRepeatableAsync()
    {
        var merchant = this._gameConfiguration.Monsters.Single(m => m.Number == MedalMerchantConfiguration.DefaultMerchantNumber);
        var lorencia = this._gameConfiguration.Maps.Single(m => m.Number == 0);
        lorencia.MonsterSpawns.Remove(lorencia.MonsterSpawns.Single(s => s.MonsterDefinition == merchant));
        var potion = this._context.CreateNew<Item>();
        potion.Definition = this._gameConfiguration.Items.First(i => i.Group == 14 && i.Number == 3);
        potion.ItemSlot = 100;
        merchant.MerchantStore!.Items.Add(potion);
        merchant.IsActive = false;

        var update = new ClassicMedalMerchantUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(merchant.IsActive, Is.True);
        Assert.That(lorencia.MonsterSpawns.Count(s => s.MonsterDefinition == merchant), Is.EqualTo(1));
        Assert.That(merchant.MerchantStore.Items.Select(i => (int)i.Level).Order(), Is.EqualTo(new[] { 10, 11 }));
        Assert.That(this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MedalMerchantPlugIn).GUID).IsActive, Is.True);
    }

    /// <summary>
    /// Tests that the Devil Square, the Blood Castle and the Team vs Team give only silver medals: 4 for the 1st place of the
    /// Devil Square, 3 for the winner of the Blood Castle and the best player of the winning Team vs Team team,
    /// and that there is no "/medals" chat command.
    /// </summary>
    [Test]
    public void EventsGiveSilverMedalsOnly()
    {
        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MiniGameMedalsPlugIn).GUID);
        var configuration = plugIn.GetConfiguration<MiniGameMedalsConfiguration>(referenceHandler)!;
        var teamVsTeam = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.TeamVsTeam.TeamVsTeamPlugIn).GUID)
            .GetConfiguration<MUnique.OpenMU.GameLogic.TeamVsTeam.TeamVsTeamConfiguration>(referenceHandler)!;

        Assert.That(configuration.DevilSquareWinnerSilverMedals, Is.EqualTo(4));
        Assert.That(configuration.DevilSquareRunnerUpSilverMedals, Is.EqualTo(2));
        Assert.That(configuration.BloodCastleWinnerSilverMedals, Is.EqualTo(3));
        Assert.That(configuration.BloodCastlePartySilverMedals, Is.EqualTo(1));
        Assert.That(teamVsTeam.MostValuablePlayerSilverMedals, Is.EqualTo(3));
        Assert.That(teamVsTeam.WinnerSilverMedals, Is.EqualTo(1));
        Assert.That(this._gameConfiguration.PlugInConfigurations.Any(c => c.TypeId == ClassicSilverMedalRewardsUpdatePlugIn.RemovedMedalsChatCommandPlugInId), Is.False);
    }

    /// <summary>
    /// Tests that the silver medal rewards update sets the winner rewards of a configuration from before the update,
    /// keeps its other rewards, replaces the points of the Team vs Team by silver medals and keeps its other settings,
    /// removes the configuration of the former "/medals" chat command plugin, and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task SilverMedalRewardsUpdateIsRepeatableAsync()
    {
        var referenceHandler = new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler();
        var teamVsTeamPlugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MUnique.OpenMU.GameLogic.TeamVsTeam.TeamVsTeamPlugIn).GUID);
        var originalTeamVsTeam = teamVsTeamPlugIn.CustomConfiguration;
        var teamVsTeam = teamVsTeamPlugIn.GetConfiguration<MUnique.OpenMU.GameLogic.TeamVsTeam.TeamVsTeamConfiguration>(referenceHandler)!;

        // The silver medals of the Team vs Team read as 0 in a configuration from before the update.
        teamVsTeam.MostValuablePlayerSilverMedals = 0;
        teamVsTeam.WinnerSilverMedals = 0;
        teamVsTeam.MinimumPlayers = 6;
        teamVsTeamPlugIn.SetConfiguration(teamVsTeam, referenceHandler);

        var plugIn = this._gameConfiguration.PlugInConfigurations.Single(c => c.TypeId == typeof(MiniGameMedalsPlugIn).GUID);
        var original = plugIn.CustomConfiguration;
        plugIn.CustomConfiguration = """
            {
              "$id": "",
              "DevilSquareRunnerUpSilverMedals": 5,
              "BloodCastlePartySilverMedals": 1,
              "KillsForSilverMedal": 50,
              "Offers": {
                "$id": "",
                "$values": [
                  { "$id": "", "Key": "box", "Name": "Box of Kundun +2", "GoldMedals": 1, "SilverMedals": 0, "ItemGroup": 14, "ItemNumber": 11, "ItemLevel": 9 }
                ]
              }
            }
            """;
        var removed = this._context.CreateNew<PlugInConfiguration>();
        removed.TypeId = ClassicSilverMedalRewardsUpdatePlugIn.RemovedMedalsChatCommandPlugInId;
        removed.IsActive = true;
        this._gameConfiguration.PlugInConfigurations.Add(removed);
        var count = this._gameConfiguration.PlugInConfigurations.Count;

        try
        {
            var update = new ClassicSilverMedalRewardsUpdatePlugIn();
            await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
            var afterFirst = plugIn.CustomConfiguration;
            await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

            var configuration = plugIn.GetConfiguration<MiniGameMedalsConfiguration>(new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler())!;
            Assert.That(configuration.DevilSquareWinnerSilverMedals, Is.EqualTo(4));
            Assert.That(configuration.BloodCastleWinnerSilverMedals, Is.EqualTo(3));
            Assert.That(configuration.DevilSquareRunnerUpSilverMedals, Is.EqualTo(5), "a customized reward is kept");
            Assert.That(plugIn.CustomConfiguration, Does.Not.Contain("Offers"));
            Assert.That(plugIn.CustomConfiguration, Is.EqualTo(afterFirst));
            Assert.That(this._gameConfiguration.PlugInConfigurations, Does.Not.Contain(removed));
            Assert.That(this._gameConfiguration.PlugInConfigurations, Has.Count.EqualTo(count - 1));

            var updatedTeamVsTeam = teamVsTeamPlugIn.GetConfiguration<MUnique.OpenMU.GameLogic.TeamVsTeam.TeamVsTeamConfiguration>(referenceHandler)!;
            Assert.That(updatedTeamVsTeam.MostValuablePlayerSilverMedals, Is.EqualTo(3));
            Assert.That(updatedTeamVsTeam.WinnerSilverMedals, Is.EqualTo(1));
            Assert.That(updatedTeamVsTeam.MinimumPlayers, Is.EqualTo(6), "a customized setting is kept");
            Assert.That(updatedTeamVsTeam.Timetable, Is.EqualTo(new[] { ClassicEvents.TeamVsTeamTime }));
        }
        finally
        {
            plugIn.CustomConfiguration = original;
            teamVsTeamPlugIn.CustomConfiguration = originalTeamVsTeam;
        }
    }

    /// <summary>
    /// Tests that every map from Lorencia to Icarus has 35 farm spots with 8 monsters on a tiny square
    /// of walkable tiles outside of the safezone, apart from each other.
    /// </summary>
    [Test]
    public void FarmSpotsAreOnWalkableSquares()
    {
        foreach (var mapNumber in ClassicFarmSpots.MapNumbers)
        {
            var map = this._gameConfiguration.Maps.Single(m => m.Number == mapNumber && m.Discriminator == 0);
            var terrain = new GameMapTerrain(map);
            var spots = map.MonsterSpawns.Where(s => ClassicFarmSpots.IsFarmSpot(s) && !ClassicFarmSpots.IsFixedSpot(s)).ToList();

            Assert.That(spots, Has.Count.EqualTo(ClassicFarmSpots.SpotsPerMap), map.Name.ValueInNeutralLanguage);
            Assert.That(spots.Select(s => s.MonsterDefinition).Distinct().Count(), Is.GreaterThan(1), map.Name.ValueInNeutralLanguage);
            foreach (var (a, b) in spots.SelectMany((a, i) => spots.Skip(i + 1).Select(b => (a, b))))
            {
                var dx = (a.X1 + a.X2 - b.X1 - b.X2) / 2.0;
                var dy = (a.Y1 + a.Y2 - b.Y1 - b.Y2) / 2.0;
                Assert.That(Math.Sqrt((dx * dx) + (dy * dy)), Is.GreaterThanOrEqualTo(ClassicFarmSpots.MinimumDistanceBetweenSpots));
            }

            foreach (var spot in spots)
            {
                TestContext.Out.WriteLine($"{map.Name.ValueInNeutralLanguage}: {spot.MonsterDefinition!.Designation.ValueInNeutralLanguage} ({spot.MonsterDefinition.Number}) x {spot.Quantity} at {(spot.X1 + spot.X2) / 2}/{(spot.Y1 + spot.Y2) / 2}");
                Assert.That(spot.MonsterDefinition.IsActive, Is.True);
                Assert.That(spot.Quantity, Is.EqualTo(ClassicFarmSpots.MonstersPerSpot));
                Assert.That(spot.X2 - spot.X1, Is.EqualTo(2 * ClassicFarmSpots.Radius));
                var free = 0;
                for (var x = spot.X1; x <= spot.X2; x++)
                {
                    for (var y = spot.Y1; y <= spot.Y2; y++)
                    {
                        free += terrain.WalkMap[x, y] && !terrain.SafezoneMap[x, y] ? 1 : 0;
                    }
                }

                Assert.That(free, Is.GreaterThanOrEqualTo(ClassicFarmSpots.MinimumFreeTiles));
            }
        }
    }

    /// <summary>
    /// Tests that the hand-picked farm spots exist with their monster on walkable tiles.
    /// </summary>
    [Test]
    public void FixedFarmSpotsAreOnWalkableTiles()
    {
        foreach (var fixedSpot in ClassicFarmSpots.FixedSpots)
        {
            var map = this._gameConfiguration.Maps.Single(m => m.Number == fixedSpot.MapNumber && m.Discriminator == 0);
            var terrain = new GameMapTerrain(map);
            var spot = map.MonsterSpawns.Single(s => ClassicFarmSpots.IsFixedSpot(s) && s.X1 + ClassicFarmSpots.Radius == fixedSpot.X && s.Y1 + ClassicFarmSpots.Radius == fixedSpot.Y);
            Assert.That(spot.MonsterDefinition!.Number, Is.EqualTo(fixedSpot.MonsterNumber));
            Assert.That(spot.Quantity, Is.EqualTo(ClassicFarmSpots.MonstersPerSpot));
            var free = 0;
            for (var x = spot.X1; x <= spot.X2; x++)
            {
                for (var y = spot.Y1; y <= spot.Y2; y++)
                {
                    free += terrain.WalkMap[x, y] && !terrain.SafezoneMap[x, y] ? 1 : 0;
                }
            }

            TestContext.Out.WriteLine($"{map.Name.ValueInNeutralLanguage}: {spot.MonsterDefinition.Designation.ValueInNeutralLanguage} at {fixedSpot.X}/{fixedSpot.Y}: {free} free tiles");
            Assert.That(free, Is.GreaterThan(0), $"{fixedSpot}");
        }
    }

    /// <summary>
    /// Tests that Arena farm spots use normal monsters and leave space around Hell Maine.
    /// </summary>
    [Test]
    public void ArenaFarmSpotsLeaveRoomForHellMaine()
    {
        var arena = this._gameConfiguration.Maps.Single(m => m.Number == 6 && m.Discriminator == 0);
        var hellMaine = arena.MonsterSpawns.Single(s => s.MonsterDefinition?.Number == 309);
        var spots = arena.MonsterSpawns.Where(ClassicFarmSpots.IsFarmSpot).ToList();
        var terrain = new GameMapTerrain(arena);

        Assert.That(spots, Has.Count.EqualTo(25));
        Assert.That(spots.Select(s => s.MonsterDefinition!.Number).Distinct(), Is.EquivalentTo(new short[] { 45, 47, 48, 57, 58, 60, 61, 62, 69, 70, 71, 72, 73, 74, 75 }));
        foreach (var spot in spots)
        {
            Assert.That(spot.MonsterDefinition!.IsActive, Is.True);
            Assert.That(spot.WaveNumber, Is.Zero);
            Assert.That(spot.SpawnTrigger, Is.EqualTo(SpawnTrigger.Automatic));
            for (var x = (int)spot.X1; x <= spot.X2; x++)
            {
                for (var y = (int)spot.Y1; y <= spot.Y2; y++)
                {
                    Assert.That(terrain.WalkMap[x, y] && !terrain.SafezoneMap[x, y], Is.True, $"{x}/{y}");
                    var dx = x - hellMaine.X1;
                    var dy = y - hellMaine.Y1;
                    Assert.That((dx * dx) + (dy * dy), Is.GreaterThanOrEqualTo(20 * 20), $"{x}/{y}");
                }
            }
        }
    }

    /// <summary>
    /// Tests that each of the walled Arena pens can be reached on foot from the place where players arrive in the Arena.
    /// </summary>
    [Test]
    public void ArenaPensCanBeEntered()
    {
        var arena = this._gameConfiguration.Maps.Single(m => m.Number == 6 && m.Discriminator == 0);
        AssertArenaPensCanBeEntered(arena);
    }

    /// <summary>
    /// Tests that the update opens the entrances into the Arena pens on an existing classic database
    /// which still has the original terrain, and that applying it again changes nothing.
    /// </summary>
    [Test]
    public async Task UpdateOpensArenaPenEntrancesAsync()
    {
        var arena = this._gameConfiguration.Maps.Single(m => m.Number == 6 && m.Discriminator == 0);
        var original = arena.TerrainData!.ToArray();
        foreach (var tile in ClassicArenaPens.EntranceTiles)
        {
            original[ClassicArenaPens.GetOffset(tile)] |= (byte)TerrainAttributeType.Blocked;
        }

        arena.TerrainData = original;
        Assert.That(new GameMapTerrain(arena).WalkMap[53, 39], Is.False);

        var update = new ClassicArenaPenEntrancesUpdatePlugIn();
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
        var updated = arena.TerrainData;
        await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);

        Assert.That(arena.TerrainData, Is.SameAs(updated));
        AssertArenaPensCanBeEntered(arena);
    }

    /// <summary>
    /// Tests that the farm spot update adds the spots once, also when it's applied twice.
    /// </summary>
    [Test]
    public async Task FarmSpotsUpdateIsRepeatableAsync()
    {
        var lorencia = this._gameConfiguration.Maps.Single(m => m.Number == 0);
        var lorenciaSpot = lorencia.MonsterSpawns.First(ClassicFarmSpots.IsFarmSpot);
        var arena = this._gameConfiguration.Maps.Single(m => m.Number == 6 && m.Discriminator == 0);
        var arenaSpots = arena.MonsterSpawns.Where(ClassicFarmSpots.IsFarmSpot).ToList();
        var arenaSpotIds = arenaSpots.Select(s => s.GetId()).ToList();
        foreach (var spot in arenaSpots)
        {
            arena.MonsterSpawns.Remove(spot);
        }

        var count = this._gameConfiguration.Maps.Sum(m => m.MonsterSpawns.Count);

        var update = new ClassicFarmSpotsUpdatePlugIn();
        var updates = await this._context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false);
        var installed = updates.Single(u => u.Key == update.Key);
        installed.Version = 3;
        var originalQuantity = lorenciaSpot.Quantity;
        lorenciaSpot.Quantity--;
        try
        {
            for (var run = 0; run < 2; run++)
            {
                await update.ApplyUpdateAsync(this._context, this._gameConfiguration).ConfigureAwait(false);
                Assert.That(installed.Version, Is.EqualTo(4));
                Assert.That(lorenciaSpot.Quantity, Is.EqualTo(originalQuantity - 1));
                Assert.That(arena.MonsterSpawns.Where(ClassicFarmSpots.IsFarmSpot).Select(s => s.GetId()), Is.EquivalentTo(arenaSpotIds));
                Assert.That(this._gameConfiguration.Maps.Sum(m => m.MonsterSpawns.Count), Is.EqualTo(count + arenaSpots.Count));
            }
        }
        finally
        {
            lorenciaSpot.Quantity = originalQuantity;
        }
    }

    private void AssertCastleSiegePlugInsAndSuppliersAreActive()
    {
        var typeIds = ClassicPlugInDefaults.GetCastleSiegePlugInTypes().Select(t => t.GUID).ToHashSet();
        Assert.That(typeIds, Does.Contain(typeof(MUnique.OpenMU.GameLogic.CastleSiege.CastleSiegePlugIn).GUID));
        Assert.That(typeIds, Does.Contain(typeof(MUnique.OpenMU.GameLogic.CastleSiege.CastleSiegeRegistrationGuardsmanTalkPlugIn).GUID));
        Assert.That(typeIds, Does.Contain(new Guid("2438FBA6-4962-4BC7-8784-69F89AB53D8F")), "CastleSiegeGroupHandlerPlugIn");
        Assert.That(typeIds, Does.Contain(new Guid("3E5E1A6A-A064-4F3E-8E96-B52860931601")), "CastleSiegeStatusHandlerPlugIn");
        var inactive = this._gameConfiguration.PlugInConfigurations.Where(c => typeIds.Contains(c.TypeId) && !c.IsActive).Select(c => c.TypeId).ToList();
        Assert.That(inactive, Is.Empty);
        var suppliers = this._gameConfiguration.Monsters.Where(m => ClassicCastleSiege.SupplierNumbers.Contains(m.Number)).ToList();
        Assert.That(suppliers, Has.Count.EqualTo(2));
        Assert.That(suppliers.All(m => m.IsActive), Is.True);
    }

    private void AssertCastleSiegeRewardsAndSignOfLordDrop()
    {
        var castleSiege = this._gameConfiguration.CastleSiegeConfiguration!;
        var dropGroups = this._gameConfiguration.DropItemGroups.Where(g => g.GetId() == ClassicCastleSiege.GetSignOfLordDropId()).ToList();
        var lostTower = this._gameConfiguration.Maps.Single(m => m.Number == 4);
        Assert.Multiple(() =>
        {
            Assert.That(castleSiege.RewardItemDefinition, Is.Not.Null);
            Assert.That((castleSiege.RewardItemDefinition!.Group, castleSiege.RewardItemDefinition.Number), Is.EqualTo(((byte)14, (short)11)), "Box of Luck");
            Assert.That(castleSiege.RewardItemLevel, Is.EqualTo(ClassicBoxes.BoxOfKundunPlus4Level));
            Assert.That(castleSiege.RewardItemCount, Is.EqualTo(5));
            Assert.That(castleSiege.ParticipantRewardMinSeconds, Is.EqualTo(ClassicCastleSiege.ParticipantRewardMinSeconds));
            Assert.That(dropGroups, Has.Count.EqualTo(1));
            Assert.That(dropGroups[0].ItemLevel, Is.EqualTo(castleSiege.SignOfLordItemLevel));
            Assert.That(dropGroups[0].PossibleItems.Single(), Is.SameAs(castleSiege.SignOfLordItemDefinition));
            Assert.That(lostTower.DropItemGroups.Count(g => g == dropGroups[0]), Is.EqualTo(1));
        });
    }

    private void AssertJewelryDrops()
    {
        var jewelry = this._gameConfiguration.Items.Where(i => ClassicJewelryDrops.Jewelry.Contains((i.Group, i.Number))).ToList();
        Assert.That(jewelry, Has.Count.EqualTo(12));
        Assert.That(jewelry, Has.All.Matches<ItemDefinition>(i => i.IsActive && !i.DropsFromMonsters));

        var normal = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == ClassicJewelryDrops.GetNormalDropGroupId());
        Assert.That(normal.ItemType, Is.EqualTo(SpecialItemType.None));
        Assert.That(normal.Chance, Is.EqualTo(ClassicJewelryDrops.JewelryDropChance));
        Assert.That(normal.MinimumMonsterLevel, Is.Null);
        Assert.That(normal.PossibleItems, Is.EquivalentTo(jewelry));

        var excellent = this._gameConfiguration.DropItemGroups.Single(g => g.GetId() == ClassicJewelryDrops.GetExcellentDropGroupId());
        Assert.That(excellent.ItemType, Is.EqualTo(SpecialItemType.Excellent));
        Assert.That(excellent.Chance, Is.EqualTo(ClassicJewelryDrops.ExcellentJewelryDropChance));
        Assert.That(excellent.MinimumMonsterLevel, Is.EqualTo(50));
        Assert.That(excellent.PossibleItems, Is.EquivalentTo(jewelry));

        var mapsWithJewels = this._gameConfiguration.Maps.Where(m => m.DropItemGroups.Any(g => g.ItemType == SpecialItemType.Jewel)).ToList();
        Assert.That(mapsWithJewels, Is.Not.Empty);
        Assert.That(mapsWithJewels, Has.All.Matches<GameMapDefinition>(m => m.DropItemGroups.Contains(normal) && m.DropItemGroups.Contains(excellent)));
    }

    private static void AssertArenaPensCanBeEntered(GameMapDefinition arena)
    {
        // A tile in the middle of each pen.
        var pens = new (int X, int Y)[] { (10, 38), (29, 38), (47, 39), (10, 56), (29, 56), (47, 57), (47, 83), (10, 74), (29, 74), (10, 92), (29, 92) };
        var terrain = new GameMapTerrain(arena);
        var start = (X: 102, Y: 116); // The arrival gate of the Arena.
        var reached = new HashSet<(int X, int Y)> { start };
        var queue = new Queue<(int X, int Y)>(reached);
        while (queue.TryDequeue(out var tile))
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    var next = (X: tile.X + dx, Y: tile.Y + dy);
                    if (next.X is >= 0 and < 256 && next.Y is >= 0 and < 256 && terrain.WalkMap[next.X, next.Y] && reached.Add(next))
                    {
                        queue.Enqueue(next);
                    }
                }
            }
        }

        Assert.That(pens, Has.All.Matches<(int X, int Y)>(reached.Contains));
    }

    private async Task SetBoxUpdateVersionAsync(ClassicBoxesUpdatePlugIn update, int version)
    {
        var updates = await this._context.GetAsync<ConfigurationUpdate>().ConfigureAwait(false);
        var entry = updates.FirstOrDefault(u => u.Key == update.Key) ?? this._context.CreateNew<ConfigurationUpdate>();
        entry.Key = update.Key;
        entry.Version = version;
    }
}
