// <copyright file="ClassicPlugInDefaults.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.HuntingRewards;
using MUnique.OpenMU.GameLogic.ItemExchange;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.PlugIns.InvasionEvents;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates and configures the plugins of the classic server:
/// resets with a rising required level, the experience rate by resets,
/// and the PvP spots with prestige. The cash shop, the hunting rewards (/jewels, /hunt), Chaos Castle,
/// the White Wizard and Red Dragon invasions and the Happy Hour are deactivated.
/// </summary>
public static class ClassicPlugInDefaults
{
    /// <summary>
    /// The maximum reset count which the server reset limit reaches.
    /// </summary>
    public const int MaximumResets = 55;

    /// <summary>
    /// The points per reset, which are multiplied with the reset count: the first reset gives 500 points, the second 1000, and so on.
    /// </summary>
    public const int ClassicPointsPerReset = 500;

    /// <summary>
    /// The lowest drop level of the excellent items which the monsters of Tarkan and Icarus drop:
    /// nothing below the Legendary, Dragon and Guardian sets, e.g. the Legendary Helm and the Guardian Gloves (both 50).
    /// The Legendary Gloves, Boots and Shield and the Legendary Sword (44 to 48) are below it, together with the Plate and Spirit sets.
    /// </summary>
    public const int HighMapsMinimumExcellentDropLevel = 50;

    /// <summary>
    /// The highest drop level of the excellent items which the monsters of Tarkan and Icarus drop:
    /// just below the Grand Soul and Storm Crow Gloves and the Staff of Resurrection (70), so nothing of the
    /// Grand Soul, Dark Phoenix, Black Dragon, Divine and Storm Crow sets and no weapons of their level drops there.
    /// </summary>
    public const int HighMapsMaximumExcellentDropLevel = 69;

    /// <summary>
    /// The maximum number of accounts which are logged in at the same time from one IP address,
    /// including the accounts which play in the offline mode (<c>/offlevel</c>).
    /// </summary>
    public const int MaximumAccountsPerIp = 2;

    /// <summary>
    /// The maximum number of accounts of one IP address which take part in the same event, e.g. the Devil Square.
    /// </summary>
    public const int MaximumAccountsPerIpInEvents = 1;

    /// <summary>
    /// The plugins which the classic server doesn't use: Chaos Castle, the White Wizard and Red Dragon invasions and the Happy Hour.
    /// </summary>
    private static readonly Type[] InactivePlugIns =
    [
        typeof(ChaosCastleStartPlugIn),
        typeof(WhiteWizardInvasionPlugIn),
        typeof(RedDragonInvasionPlugIn),
        typeof(HappyHourPlugIn),
    ];

    /// <summary>
    /// The chat commands which are disabled by default, but used by the classic server:
    /// the stat commands of the players (e.g. <c>/str 100</c>), <c>/openware</c> and the game master commands for the level and the NPCs.
    /// </summary>
    private static readonly Type[] ActivePlugIns =
    [
        typeof(AddStrengthStatChatCommandPlugIn),
        typeof(AddAgilityStatChatCommandPlugIn),
        typeof(AddVitalityStatChatCommandPlugIn),
        typeof(AddEnergyStatChatCommandPlugIn),
        typeof(AddCommandStatChatCommandPlugIn),
        typeof(OpenWarehouseChatCommandPlugIn),
        typeof(GetLevelChatCommandPlugIn),
        typeof(SetLevelChatCommandPlugIn),
        typeof(NpcChatCommandPlugIn),
    ];

    /// <summary>
    /// Applies the defaults to the plugin configurations of the game configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void Apply(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        if (Find<ResetFeaturePlugIn>(gameConfiguration) is { } reset)
        {
            reset.IsActive = true;
            reset.SetConfiguration(CreateResetConfiguration(), referenceHandler);
        }

        if (Find<ResetExperienceRatePlugIn>(gameConfiguration) is { } experience)
        {
            experience.IsActive = true;
            experience.SetConfiguration(ResetExperienceRateConfiguration.CreateDefault(), referenceHandler);
        }

        // The hunting rewards are configured, but not offered; they can be activated in the admin panel.
        if (Find<HuntingJewelPlugIn>(gameConfiguration) is { } huntingJewels)
        {
            huntingJewels.IsActive = false;
            huntingJewels.SetConfiguration(new HuntingJewelConfiguration(), referenceHandler);
        }

        if (Find<HuntingPointsPlugIn>(gameConfiguration) is { } huntingPoints)
        {
            huntingPoints.IsActive = false;
            huntingPoints.SetConfiguration(HuntingPointsConfiguration.CreateDefault(), referenceHandler);
        }

        if (Find<ItemExchangeChatCommandPlugIn>(gameConfiguration) is { } itemExchange)
        {
            itemExchange.IsActive = true;
            itemExchange.SetConfiguration(new ItemExchangeConfiguration(), referenceHandler);
        }

        if (Find<PvpSpotsPlugIn>(gameConfiguration) is { } pvp)
        {
            var configuration = new PvpSpotsConfiguration();
            foreach (var spot in ClassicPvpSpots.Create(gameConfiguration))
            {
                configuration.Spots.Add(spot);
            }

            pvp.IsActive = true;
            pvp.SetConfiguration(configuration, referenceHandler);
        }

        // A classic server has no cash shop: the client's shop (X key) is refused and no points are granted.
        if (Find<CashShopFeaturePlugIn>(gameConfiguration) is { } cashShop)
        {
            cashShop.IsActive = false;
        }

        if (Find<StartingInventoryExtensionsPlugIn>(gameConfiguration) is { } inventoryExtensions)
        {
            inventoryExtensions.IsActive = true;
            inventoryExtensions.SetConfiguration(new StartingInventoryExtensionsConfiguration(), referenceHandler);
        }

        if (Find<ItemLevelLimitPlugIn>(gameConfiguration) is { } itemLevelLimit)
        {
            itemLevelLimit.IsActive = true;
            itemLevelLimit.SetConfiguration(new ItemLevelLimitConfiguration(), referenceHandler);
        }

        if (Find<MedalMerchantPlugIn>(gameConfiguration) is { } medalMerchant)
        {
            medalMerchant.IsActive = true;
            medalMerchant.SetConfiguration(MedalMerchantConfiguration.CreateDefault(), referenceHandler);
        }

        if (Find<UnlockedCharacterClassesPlugIn>(gameConfiguration) is { } unlockedClasses)
        {
            unlockedClasses.IsActive = true;
            unlockedClasses.SetConfiguration(new UnlockedCharacterClassesConfiguration(), referenceHandler);
        }

        if (Find<MapExcellentDropLevelsPlugIn>(gameConfiguration) is { } excellentDropLevels)
        {
            excellentDropLevels.IsActive = true;
            excellentDropLevels.SetConfiguration(CreateExcellentDropLevelsConfiguration(), referenceHandler);
        }

        if (Find<MaximumConnectionsPerIpPlugIn>(gameConfiguration) is { } maximumAccountsPerIp)
        {
            maximumAccountsPerIp.IsActive = true;
            maximumAccountsPerIp.SetConfiguration(CreateMaximumAccountsPerIpConfiguration(), referenceHandler);
        }

        if (Find<EventAccountsPerIpPlugIn>(gameConfiguration) is { } eventAccountsPerIp)
        {
            eventAccountsPerIp.IsActive = true;
            eventAccountsPerIp.SetConfiguration(CreateEventAccountsPerIpConfiguration(), referenceHandler);
        }

        SetActive(gameConfiguration, false, InactivePlugIns);
        SetActive(gameConfiguration, true, ActivePlugIns);
    }

    /// <summary>
    /// Creates the configuration of the <see cref="EventAccountsPerIpPlugIn"/> with <see cref="MaximumAccountsPerIpInEvents"/> accounts per IP address.
    /// </summary>
    /// <returns>The configuration of the <see cref="EventAccountsPerIpPlugIn"/>.</returns>
    public static EventAccountsPerIpConfiguration CreateEventAccountsPerIpConfiguration()
        => new() { MaximumAccountsPerIp = MaximumAccountsPerIpInEvents };

    /// <summary>
    /// Activates the <see cref="EventAccountsPerIpPlugIn"/> with <see cref="MaximumAccountsPerIpInEvents"/> accounts per IP address,
    /// and creates its plugin configuration if it doesn't exist yet.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="context">The context which creates the plugin configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ActivateEventAccountsPerIp(GameConfiguration gameConfiguration, IContext context, ReferenceHandler referenceHandler)
    {
        if (Find<EventAccountsPerIpPlugIn>(gameConfiguration) is not { } plugInConfiguration)
        {
            var typeId = typeof(EventAccountsPerIpPlugIn).GUID;
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(CreateEventAccountsPerIpConfiguration(), referenceHandler);
    }

    /// <summary>
    /// Creates the configuration of the <see cref="MaximumConnectionsPerIpPlugIn"/> with <see cref="MaximumAccountsPerIp"/> accounts per IP address.
    /// </summary>
    /// <returns>The configuration of the <see cref="MaximumConnectionsPerIpPlugIn"/>.</returns>
    public static MaximumConnectionsPerIpPlugInConfiguration CreateMaximumAccountsPerIpConfiguration()
        => new() { MaximumConnectionsPerIp = MaximumAccountsPerIp };

    /// <summary>
    /// Activates the <see cref="MaximumConnectionsPerIpPlugIn"/> with <see cref="MaximumAccountsPerIp"/> accounts per IP address,
    /// and creates its plugin configuration if it doesn't exist yet.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="context">The context which creates the plugin configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ActivateMaximumAccountsPerIp(GameConfiguration gameConfiguration, IContext context, ReferenceHandler referenceHandler)
    {
        if (Find<MaximumConnectionsPerIpPlugIn>(gameConfiguration) is not { } plugInConfiguration)
        {
            var typeId = typeof(MaximumConnectionsPerIpPlugIn).GUID;
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(CreateMaximumAccountsPerIpConfiguration(), referenceHandler);
    }

    /// <summary>
    /// Creates the excellent item drop levels of the classic server: in Tarkan and Icarus, the monsters drop
    /// excellent items between <see cref="HighMapsMinimumExcellentDropLevel"/> and <see cref="HighMapsMaximumExcellentDropLevel"/>.
    /// Normal items still drop by the monster level.
    /// </summary>
    /// <returns>The configuration of the <see cref="MapExcellentDropLevelsPlugIn"/>.</returns>
    public static MapExcellentDropLevelsConfiguration CreateExcellentDropLevelsConfiguration()
    {
        var configuration = new MapExcellentDropLevelsConfiguration();
        foreach (var mapNumber in new short[] { Version095d.Maps.Tarkan.Number, Version095d.Maps.Icarus.Number })
        {
            configuration.Maps.Add(new()
            {
                MapNumber = mapNumber,
                MinimumDropLevel = HighMapsMinimumExcellentDropLevel,
                MaximumDropLevel = HighMapsMaximumExcellentDropLevel,
            });
        }

        return configuration;
    }

    /// <summary>
    /// Activates the <see cref="MapExcellentDropLevelsPlugIn"/> with the excellent item drop levels of Tarkan and Icarus,
    /// see <see cref="CreateExcellentDropLevelsConfiguration"/>, and creates its plugin configuration if it doesn't exist yet.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="context">The context which creates the plugin configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ActivateExcellentDropLevels(GameConfiguration gameConfiguration, IContext context, ReferenceHandler referenceHandler)
    {
        if (Find<MapExcellentDropLevelsPlugIn>(gameConfiguration) is not { } plugInConfiguration)
        {
            var typeId = typeof(MapExcellentDropLevelsPlugIn).GUID;
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(CreateExcellentDropLevelsConfiguration(), referenceHandler);
    }

    /// <summary>
    /// Activates the <see cref="UnlockedCharacterClassesPlugIn"/>, so every account can create a Magic Gladiator,
    /// and creates its plugin configuration if it doesn't exist yet.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="context">The context which creates the plugin configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ActivateUnlockedCharacterClasses(GameConfiguration gameConfiguration, IContext context, ReferenceHandler referenceHandler)
    {
        if (Find<UnlockedCharacterClassesPlugIn>(gameConfiguration) is not { } plugInConfiguration)
        {
            var typeId = typeof(UnlockedCharacterClassesPlugIn).GUID;
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(new UnlockedCharacterClassesConfiguration(), referenceHandler);
    }

    /// <summary>
    /// Activates the <see cref="MedalMerchantPlugIn"/> with its default prices,
    /// and creates its plugin configuration if it doesn't exist yet.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="context">The context which creates the plugin configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ActivateMedalMerchant(GameConfiguration gameConfiguration, IContext context, ReferenceHandler referenceHandler)
    {
        if (Find<MedalMerchantPlugIn>(gameConfiguration) is not { } plugInConfiguration)
        {
            var typeId = typeof(MedalMerchantPlugIn).GUID;
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(MedalMerchantConfiguration.CreateDefault(), referenceHandler);
    }

    /// <summary>
    /// Resets the boss list of the <see cref="BossMapMarkersPlugIn"/>, which is also the list of the <see cref="BossesChatCommandPlugIn"/>,
    /// to their defaults, which are only the field bosses: Balrog, Hydra, Zaikan, Death Beam Knight, Dark Phoenix and Hell Maine.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ResetBossLists(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        Find<BossMapMarkersPlugIn>(gameConfiguration)?.SetConfiguration(new BossMapMarkersConfiguration(), referenceHandler);
    }

    /// <summary>
    /// Activates the <see cref="ItemLevelLimitPlugIn"/> with its default limit (+11),
    /// and creates its plugin configuration if it doesn't exist yet.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="context">The context which creates the plugin configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ActivateItemLevelLimit(GameConfiguration gameConfiguration, IContext context, ReferenceHandler referenceHandler)
    {
        if (Find<ItemLevelLimitPlugIn>(gameConfiguration) is not { } plugInConfiguration)
        {
            var typeId = typeof(ItemLevelLimitPlugIn).GUID;
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(new ItemLevelLimitConfiguration(), referenceHandler);
    }

    /// <summary>
    /// Activates the <see cref="StartingInventoryExtensionsPlugIn"/> with all inventory extensions,
    /// and creates its plugin configuration if it doesn't exist yet.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="context">The context which creates the plugin configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ActivateStartingInventoryExtensions(GameConfiguration gameConfiguration, IContext context, ReferenceHandler referenceHandler)
    {
        if (Find<StartingInventoryExtensionsPlugIn>(gameConfiguration) is not { } plugInConfiguration)
        {
            var typeId = typeof(StartingInventoryExtensionsPlugIn).GUID;
            plugInConfiguration = context.CreateNew<PlugInConfiguration>();
            plugInConfiguration.SetGuid(typeId);
            plugInConfiguration.TypeId = typeId;
            gameConfiguration.PlugInConfigurations.Add(plugInConfiguration);
        }

        plugInConfiguration.IsActive = true;
        plugInConfiguration.SetConfiguration(new StartingInventoryExtensionsConfiguration(), referenceHandler);
    }

    /// <summary>
    /// Creates the reset configuration of the classic server:
    /// the first reset requires level 350, each further one 10 more, up to 400 from the fifth reset on.
    /// The character starts again at level 10. The resets are limited to <see cref="MaximumResets"/>; there is no daily server reset limit,
    /// because the experience rate by resets already slows the characters down. It can be started in the admin panel (server reset limit start).
    /// The stats are reset and the character gets <see cref="ClassicPointsPerReset"/> points per reset count, e.g. 1500 points after the third reset.
    /// </summary>
    /// <returns>The reset configuration.</returns>
    public static ResetConfiguration CreateResetConfiguration()
    {
        var configuration = new ResetConfiguration
        {
            RequiredLevel = 350,
            RequiredLevelIncreasePerReset = 10,
            MaximumRequiredLevel = 400,
            LevelAfterReset = 10,
            RequiredMoney = 0,
            MultiplyRequiredMoneyByResetCount = false,
            ResetStats = true,
            ReplacePointsPerReset = true,
            MultiplyPointsByResetCount = true,
            PointsPerReset = ClassicPointsPerReset,
            ResetLimit = MaximumResets,
            ServerResetLimitStart = null,
            ServerResetLimitInitial = 1,
            ServerResetLimitIncreasePerDay = 1,
            MoveHome = true,
            LogOut = true,
        };

        return configuration;
    }

    /// <summary>
    /// Replaces the former default reset points of the <see cref="ResetFeaturePlugIn"/> (tiers of 2000, 1500 and 1000 points)
    /// by <see cref="ClassicPointsPerReset"/> points multiplied with the reset count. The other reset settings are kept.
    /// A configuration whose points were changed by hand is kept.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    /// <returns><c>True</c>, if the configuration was replaced.</returns>
    public static bool ReplaceTieredResetPoints(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        if (Find<ResetFeaturePlugIn>(gameConfiguration) is not { } plugInConfiguration
            || plugInConfiguration.GetConfiguration<ResetConfiguration>(referenceHandler) is not { } configuration
            || !HasFormerDefaultPointTiers(configuration))
        {
            return false;
        }

        configuration.PointsTiers.Clear();
        configuration.PointsPerReset = ClassicPointsPerReset;
        configuration.MultiplyPointsByResetCount = true;
        configuration.ReplacePointsPerReset = true;
        plugInConfiguration.SetConfiguration(configuration, referenceHandler);
        return true;
    }

    private static bool HasFormerDefaultPointTiers(ResetConfiguration configuration)
    {
        var tiers = configuration.PointsTiers
            .OrderBy(t => t.MinimumResetCount)
            .Select(t => (t.MinimumResetCount, t.PointsGranted))
            .ToList();
        return tiers.SequenceEqual([(1, 2000), (2, 1500), (5, 1000)]);
    }

    /// <summary>
    /// Replaces the first default configuration of the <see cref="ResetExperienceRatePlugIn"/>, which held
    /// absolute rates (540 at zero resets) and therefore multiplied the configured experience rate by up to 540,
    /// with the current default of relative multipliers. A configuration which was changed by hand is kept.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    /// <returns><c>True</c>, if the configuration was replaced.</returns>
    public static bool ReplaceAbsoluteResetExperienceRates(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        const float firstAbsoluteDefaultRate = 540;
        if (Find<ResetExperienceRatePlugIn>(gameConfiguration) is not { } plugInConfiguration
            || plugInConfiguration.GetConfiguration<ResetExperienceRateConfiguration>(referenceHandler) is not { } configuration
            || configuration.GetMultiplier(0) != firstAbsoluteDefaultRate)
        {
            return false;
        }

        plugInConfiguration.SetConfiguration(ResetExperienceRateConfiguration.CreateDefault(), referenceHandler);
        return true;
    }

    private static void SetActive(GameConfiguration gameConfiguration, bool isActive, IEnumerable<Type> plugInTypes)
    {
        foreach (var plugInType in plugInTypes)
        {
            if (gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == plugInType.GUID) is { } plugIn)
            {
                plugIn.IsActive = isActive;
            }
        }
    }

    private static PlugInConfiguration? Find<TPlugIn>(GameConfiguration gameConfiguration)
        => gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(TPlugIn).GUID);
}
