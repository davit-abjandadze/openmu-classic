// <copyright file="InstancedMiniGameTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Tests for the instanced mini games (Devil Square and Blood Castle in instances): the daily limit,
/// the tier selection, the party checks, the level sync, the scaling and the slots.
/// </summary>
[TestFixture]
public class InstancedMiniGameTests
{
    /// <summary>
    /// The time zone of the classic server, Asia/Tbilisi (UTC+4, without daylight saving time).
    /// It's created here, so the tests don't depend on the time zone database of the machine.
    /// </summary>
    private static readonly TimeZoneInfo Tbilisi = TimeZoneInfo.CreateCustomTimeZone("Test/Tbilisi", TimeSpan.FromHours(4), "Tbilisi", "Tbilisi");

    /// <summary>
    /// Tests that the day changes at midnight in Tbilisi (20:00 UTC), not at midnight UTC.
    /// </summary>
    [Test]
    public void DayChangesAtMidnightInTbilisi()
    {
        var beforeMidnight = MiniGameDailyEntry.GetDay(Tbilisi, new DateTime(2026, 11, 26, 19, 59, 59, DateTimeKind.Utc));
        var atMidnight = MiniGameDailyEntry.GetDay(Tbilisi, new DateTime(2026, 11, 26, 20, 0, 0, DateTimeKind.Utc));
        var utcMidnight = MiniGameDailyEntry.GetDay(Tbilisi, new DateTime(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc));

        Assert.That(atMidnight, Is.EqualTo(beforeMidnight + 1));
        Assert.That(utcMidnight, Is.EqualTo(atMidnight));
    }

    /// <summary>
    /// Tests that an entry counts for the rest of the day in Tbilisi and that the limit resets at its midnight:
    /// the stored day of the last entry is compared with the current day.
    /// </summary>
    [Test]
    public void DailyLimitResetsAtMidnightInTbilisi()
    {
        // Entered at 13:05 in Tbilisi.
        var storedEntryDay = MiniGameDailyEntry.GetDay(Tbilisi, new DateTime(2026, 11, 26, 9, 5, 0, DateTimeKind.Utc));

        // 22:00 in Tbilisi: still the same day, so the attempt is used up.
        Assert.That(MiniGameDailyEntry.HasEnteredOnDay(storedEntryDay, MiniGameDailyEntry.GetDay(Tbilisi, new DateTime(2026, 11, 26, 18, 0, 0, DateTimeKind.Utc))), Is.True);

        // 00:00:01 in Tbilisi on the next day: available again.
        Assert.That(MiniGameDailyEntry.HasEnteredOnDay(storedEntryDay, MiniGameDailyEntry.GetDay(Tbilisi, new DateTime(2026, 11, 26, 20, 0, 1, DateTimeKind.Utc))), Is.False);

        // A character which never entered has no stored day.
        Assert.That(MiniGameDailyEntry.HasEnteredOnDay(0, storedEntryDay), Is.False);
    }

    /// <summary>
    /// Tests that the devil square and the blood castle are limited independently.
    /// </summary>
    [Test]
    public void DevilSquareAndBloodCastleHaveSeparateEntryDays()
    {
        Assert.That(MiniGameEntryStats.GetEntryDayAttribute(MiniGameType.DevilSquare), Is.Not.Null);
        Assert.That(MiniGameEntryStats.GetEntryDayAttribute(MiniGameType.BloodCastle), Is.Not.Null);
        Assert.That(MiniGameEntryStats.GetEntryDayAttribute(MiniGameType.DevilSquare), Is.Not.EqualTo(MiniGameEntryStats.GetEntryDayAttribute(MiniGameType.BloodCastle)));
        Assert.That(MiniGameEntryStats.GetEntryDayAttribute(MiniGameType.ChaosCastle), Is.Null);
    }

    /// <summary>
    /// Tests that the tier is selected by the level window of the character.
    /// </summary>
    /// <param name="level">The level of the character.</param>
    /// <param name="expectedTier">The expected tier; 0 for none.</param>
    [TestCase(9, 0)]
    [TestCase(10, 1)]
    [TestCase(50, 1)]
    [TestCase(60, 1)]
    [TestCase(61, 2)]
    [TestCase(180, 3)]
    [TestCase(181, 4)]
    [TestCase(300, 5)]
    [TestCase(360, 6)]
    [TestCase(361, 7)]
    [TestCase(400, 7)]
    [TestCase(450, 7)]
    public void TierIsSelectedByLevel(int level, int expectedTier)
    {
        var tier = InstancedMiniGameEntryRules.SelectTier(CreateTiers(), level, false);

        Assert.That(tier?.GameLevel ?? 0, Is.EqualTo(expectedTier));
    }

    /// <summary>
    /// Tests that a party plays the tier of its lowest member.
    /// </summary>
    [Test]
    public void PartyPlaysTierOfLowestMember()
    {
        var tiers = CreateTiers();
        var candidates = new[]
        {
            CreateCandidate("High", 380, tiers),
            CreateCandidate("Low", 50, tiers),
            CreateCandidate("Middle", 200, tiers),
        };

        Assert.That(InstancedMiniGameEntryRules.SelectPartyTier(candidates)?.GameLevel, Is.EqualTo(1));
    }

    /// <summary>
    /// Tests that nobody enters when one member of the party doesn't meet a requirement, and that the leader learns who and why.
    /// </summary>
    [Test]
    public void OneFailingMemberBlocksTheWholeParty()
    {
        var tiers = CreateTiers();
        var candidates = new[]
        {
            CreateCandidate("Leader", 100, tiers),
            CreateCandidate("Ready", 120, tiers),
            CreateCandidate("Tired", 110, tiers) with { HasEnteredToday = true },
        };

        var refusals = InstancedMiniGameEntryRules.Validate(candidates);

        Assert.That(refusals, Is.EqualTo(new[] { ("Tired", InstancedEntryRefusal.AlreadyEnteredToday) }));
    }

    /// <summary>
    /// Tests the reasons of each failed requirement.
    /// </summary>
    [Test]
    public void EachRequirementHasItsReason()
    {
        var tiers = CreateTiers();
        var ready = CreateCandidate("Member", 100, tiers);

        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready), Is.EqualTo(InstancedEntryRefusal.None));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { IsOnline = false }), Is.EqualTo(InstancedEntryRefusal.Offline));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { IsAlive = false }), Is.EqualTo(InstancedEntryRefusal.Dead));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { IsNearLeader = false }), Is.EqualTo(InstancedEntryRefusal.TooFarAway));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { IsInOtherEvent = true }), Is.EqualTo(InstancedEntryRefusal.InOtherEvent));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { IsPlayerKiller = true }), Is.EqualTo(InstancedEntryRefusal.PlayerKiller));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { Tier = null }), Is.EqualTo(InstancedEntryRefusal.LevelTooLow));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { IsDailyLimitTracked = false }), Is.EqualTo(InstancedEntryRefusal.NotTracked));
        Assert.That(InstancedMiniGameEntryRules.GetRefusal(ready with { HasEnteredToday = true }), Is.EqualTo(InstancedEntryRefusal.AlreadyEnteredToday));
    }

    /// <summary>
    /// Tests that a party in which all members meet the requirements can enter.
    /// </summary>
    [Test]
    public void ReadyPartyCanEnter()
    {
        var tiers = CreateTiers();
        var candidates = new[] { CreateCandidate("Leader", 100, tiers), CreateCandidate("Member", 300, tiers) };

        Assert.That(InstancedMiniGameEntryRules.Validate(candidates), Is.Empty);
    }

    /// <summary>
    /// Tests that the level sync weakens a character above the maximum level of the tier,
    /// and that removing it restores every attribute exactly.
    /// </summary>
    [Test]
    public void LevelSyncIsRestoredWhenRemoved()
    {
        var attributes = CreatePlayerLikeAttributes();
        var before = MiniGameLevelSync.SyncedAttributes.ToDictionary(a => a, a => attributes[a]);

        var factor = MiniGameLevelSync.CalculateFactor(400, 60);
        var sync = MiniGameLevelSync.Apply(attributes, factor);

        Assert.That(sync, Is.Not.Null);
        Assert.That(factor, Is.EqualTo(0.15f).Within(0.0001f));
        Assert.That(attributes[Stats.MaximumHealth], Is.EqualTo(before[Stats.MaximumHealth] * factor).Within(0.01f));
        Assert.That(attributes[Stats.MaximumPhysBaseDmg], Is.EqualTo(before[Stats.MaximumPhysBaseDmg] * factor).Within(0.01f));
        Assert.That(attributes[Stats.CurrentHealth], Is.LessThanOrEqualTo(attributes[Stats.MaximumHealth]));

        sync!.Remove();

        foreach (var (attribute, value) in before)
        {
            Assert.That(attributes[attribute], Is.EqualTo(value).Within(0.001f), attribute.Designation);
        }
    }

    /// <summary>
    /// Tests that a character within the level window isn't weakened, and that attributes without a value don't get one.
    /// </summary>
    [Test]
    public void LevelSyncOnlyWeakensCharactersAboveTheWindow()
    {
        Assert.That(MiniGameLevelSync.CalculateFactor(60, 60), Is.EqualTo(1));
        Assert.That(MiniGameLevelSync.CalculateFactor(20, 60), Is.EqualTo(1));
        Assert.That(MiniGameLevelSync.Apply(CreatePlayerLikeAttributes(), 1), Is.Null);

        var attributes = CreatePlayerLikeAttributes();
        var sync = MiniGameLevelSync.Apply(attributes, 0.5f);
        Assert.That(attributes[Stats.MinimumWizBaseDmg], Is.Zero);
        sync?.Remove();
    }

    /// <summary>
    /// Tests that the scaling for a group size is the configured one, and the biggest one for bigger groups.
    /// </summary>
    [Test]
    public void ScalingIsFoundByPlayerCount()
    {
        var scalings = InstancedMiniGameStartConfiguration.CreateDefaultPartySizeScalings();

        Assert.That(PartySizeScaling.Find(scalings, 1).MonsterCountFactor, Is.EqualTo(0.6));
        Assert.That(PartySizeScaling.Find(scalings, 1).RewardFactor, Is.EqualTo(1.5));
        Assert.That(PartySizeScaling.Find(scalings, 3).MonsterHealthFactor, Is.EqualTo(1.0));
        Assert.That(PartySizeScaling.Find(scalings, 5).MonsterHealthFactor, Is.EqualTo(1.2));
        Assert.That(PartySizeScaling.Find(scalings, 8).MonsterCountFactor, Is.EqualTo(1.3));
        Assert.That(PartySizeScaling.Find(null, 2), Is.SameAs(PartySizeScaling.Neutral));
    }

    /// <summary>
    /// Tests that the number of event monsters is scaled, but every spawn keeps at least one monster (e.g. a boss).
    /// </summary>
    [Test]
    public void SpawnQuantityIsScaledButKeepsBosses()
    {
        var map = new GameMap(new GameMapDefinition(), TimeSpan.FromMinutes(1), 8);
        map.SpawnQuantityFactor = 0.6;

        Assert.That(map.GetEventSpawnQuantity(new MonsterSpawnArea { Quantity = 10 }), Is.EqualTo(6));
        Assert.That(map.GetEventSpawnQuantity(new MonsterSpawnArea { Quantity = 1 }), Is.EqualTo(1));

        map.SpawnQuantityFactor = 1.3;
        Assert.That(map.GetEventSpawnQuantity(new MonsterSpawnArea { Quantity = 10 }), Is.EqualTo(13));
    }

    /// <summary>
    /// Tests that the start is announced once at each configured time before it.
    /// </summary>
    [Test]
    public async Task UpcomingStartIsAnnouncedFiveAndTwoMinutesBeforeAsync()
    {
        var (contextMock, notifications) = CreateAnnouncingContext(new GameConfiguration());
        var plugIn = new TestInstancedStartPlugIn(contextMock.Object)
        {
            Configuration = new DevilSquareStartConfiguration
            {
                Timetable = [new TimeOnly(13, 0)],
                AnnouncementLeadTimes = [TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(2)],
                UpcomingMessage = "Opens in {0}",
            },
        };

        // The server time zone is Tbilisi, so 13:00 is 09:00 UTC.
        var day = new DateTime(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc);
        await plugIn.AnnounceUpcomingStartAsync(contextMock.Object, day.Add(new TimeSpan(8, 54, 59))).ConfigureAwait(false);
        await plugIn.AnnounceUpcomingStartAsync(contextMock.Object, day.Add(new TimeSpan(8, 55, 0))).ConfigureAwait(false);
        await plugIn.AnnounceUpcomingStartAsync(contextMock.Object, day.Add(new TimeSpan(8, 55, 1))).ConfigureAwait(false);
        await plugIn.AnnounceUpcomingStartAsync(contextMock.Object, day.Add(new TimeSpan(8, 58, 0))).ConfigureAwait(false);
        await plugIn.AnnounceUpcomingStartAsync(contextMock.Object, day.Add(new TimeSpan(8, 59, 0))).ConfigureAwait(false);

        Assert.That(notifications, Is.EqualTo(new[] { "Opens in 5", "Opens in 2" }));
    }

    /// <summary>
    /// Tests that a start opens a slot for instanced definitions, which keeps the event active
    /// and is announced, and that the slot isn't open after its entrance duration.
    /// </summary>
    [Test]
    public async Task StartOpensSlotForInstancedDefinitionsAsync()
    {
        var mapDefinition = new GameMapDefinition { Number = 9, IsActive = true };
        var definition = new MiniGameDefinition
        {
            Type = MiniGameType.DevilSquare,
            GameLevel = 1,
            MapCreationPolicy = MiniGameMapCreationPolicy.OnePerParty,
            Entrance = new ExitGate { Map = mapDefinition },
        };
        var configurationMock = new Mock<GameConfiguration>();
        configurationMock.SetupGet(c => c.MiniGameDefinitions).Returns(new List<MiniGameDefinition> { definition });
        var (contextMock, notifications) = CreateAnnouncingContext(configurationMock.Object);
        var plugIn = new TestInstancedStartPlugIn(contextMock.Object)
        {
            Configuration = new DevilSquareStartConfiguration
            {
                EntranceDuration = TimeSpan.FromMinutes(5),
                EntranceOpenedMessage = "Open for {0}",
            },
        };

        Assert.That(plugIn.IsInstanced(contextMock.Object), Is.True);
        Assert.That(plugIn.GetOpenSlot(contextMock.Object), Is.Null);

        await plugIn.StartForTestAsync().ConfigureAwait(false);

        var slot = plugIn.GetOpenSlot(contextMock.Object);
        Assert.That(slot, Is.Not.Null);
        Assert.That(slot!.EntranceClosesUtc - slot.StartUtc, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(slot.IsEntranceOpen(slot.EntranceClosesUtc), Is.False);
        Assert.That(plugIn.IsEventActive(contextMock.Object), Is.True);
        Assert.That(notifications, Is.EqualTo(new[] { "Open for 5" }));

        await plugIn.DisposeRunningGamesAsync(contextMock.Object).ConfigureAwait(false);
        Assert.That(plugIn.GetOpenSlot(contextMock.Object), Is.Null);
    }

    /// <summary>
    /// Tests that the slot limits the number of its instances.
    /// </summary>
    [Test]
    public void SlotLimitsItsInstances()
    {
        var slot = new InstancedMiniGameSlot(MiniGameType.BloodCastle, new BloodCastleStartConfiguration { MaximumInstancesPerSlot = 2 }, DateTime.UtcNow);

        Assert.That(slot.TryReserveInstance(), Is.True);
        Assert.That(slot.TryReserveInstance(), Is.True);
        Assert.That(slot.TryReserveInstance(), Is.False);

        slot.ReleaseInstance();
        Assert.That(slot.TryReserveInstance(), Is.True);
    }

    private static IReadOnlyList<MiniGameDefinition> CreateTiers()
    {
        var windows = new[] { (10, 60), (61, 120), (121, 180), (181, 240), (241, 300), (301, 360), (361, 400) };
        return windows.Select((w, i) => new MiniGameDefinition
        {
            Type = MiniGameType.DevilSquare,
            GameLevel = (byte)(i + 1),
            MinimumCharacterLevel = w.Item1,
            MaximumCharacterLevel = w.Item2,
            MinimumSpecialCharacterLevel = w.Item1,
            MaximumSpecialCharacterLevel = w.Item2,
        }).ToList();
    }

    private static InstancedEntryCandidate CreateCandidate(string name, int level, IReadOnlyList<MiniGameDefinition> tiers)
    {
        return new InstancedEntryCandidate(
            name,
            level,
            IsOnline: true,
            IsAlive: true,
            IsNearLeader: true,
            IsInOtherEvent: false,
            IsPlayerKiller: false,
            HasEnteredToday: false,
            IsDailyLimitTracked: true,
            InstancedMiniGameEntryRules.SelectTier(tiers, level, false));
    }

    private static AttributeSystem CreatePlayerLikeAttributes()
    {
        var attributes = new AttributeSystem(
            [new StatAttribute(Stats.CurrentHealth, 2000), new StatAttribute(Stats.CurrentMana, 500)],
            [],
            []);
        attributes.AddElement(new ConstantElement(2000), Stats.MaximumHealth);
        attributes.AddElement(new ConstantElement(500), Stats.MaximumMana);
        attributes.AddElement(new ConstantElement(300), Stats.MinimumPhysBaseDmg);
        attributes.AddElement(new ConstantElement(450), Stats.MaximumPhysBaseDmg);
        attributes.AddElement(new ConstantElement(250), Stats.DefenseBase);
        attributes.AddElement(new ConstantElement(1200), Stats.AttackRatePvm);
        attributes.AddElement(new ConstantElement(600), Stats.DefenseRatePvm);
        return attributes;
    }

    private static (Mock<IGameContext> Context, List<string> Notifications) CreateAnnouncingContext(GameConfiguration configuration)
    {
        var contextMock = new Mock<IGameContext>();
        var notifications = new List<string>();
        contextMock.SetupGet(c => c.LoggerFactory).Returns(NullLoggerFactory.Instance);
        contextMock.SetupGet(c => c.Configuration).Returns(configuration);
        contextMock.SetupGet(c => c.ServerTimeZone).Returns(Tbilisi);
        var miniGames = new Mock<IMiniGameManager>();
        miniGames.Setup(m => m.GetRunningMiniGames(It.IsAny<MiniGameType>())).Returns(new List<MiniGameContext>());
        contextMock.SetupGet(c => c.MiniGames).Returns(miniGames.Object);
        contextMock.Setup(c => c.SendGlobalNotificationAsync(It.IsAny<string>()))
            .Callback<string>(notifications.Add)
            .Returns(ValueTask.CompletedTask);
        contextMock.Setup(c => c.SendGlobalNotificationAsync(It.IsAny<LocalizedString>(), It.IsAny<object?[]>()))
            .Callback<LocalizedString, object?[]>((message, arguments) => notifications.Add(string.Format(CultureInfo.InvariantCulture, message.ValueInNeutralLanguage, arguments)))
            .Returns(ValueTask.CompletedTask);
        return (contextMock, notifications);
    }

    /// <summary>
    /// A start plugin of an instanced mini game which exposes the start for tests.
    /// </summary>
    private sealed class TestInstancedStartPlugIn : MiniGameStartBasePlugIn<DevilSquareStartConfiguration, PeriodicTaskGameServerState>
    {
        private readonly IGameContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestInstancedStartPlugIn"/> class.
        /// </summary>
        /// <param name="context">The context which started states belong to.</param>
        public TestInstancedStartPlugIn(IGameContext context)
        {
            this._context = context;
        }

        /// <inheritdoc />
        public override MiniGameType Key => MiniGameType.DevilSquare;

        /// <inheritdoc />
        public override object CreateDefaultConfig() => new DevilSquareStartConfiguration();

        /// <summary>
        /// Runs the start logic.
        /// </summary>
        /// <returns>The task.</returns>
        public Task StartForTestAsync() => this.OnStartedAsync(this.GetStateByGameContext(this._context)).AsTask();

        /// <inheritdoc />
        protected override PeriodicTaskGameServerState CreateState(IGameContext gameContext) => new(this._context);
    }
}
