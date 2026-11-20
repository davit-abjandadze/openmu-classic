// <copyright file="ClassicPlugInDefaults.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.HuntingRewards;
using MUnique.OpenMU.GameLogic.ItemExchange;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates and configures the plugins of the classic server:
/// resets with a rising required level and a rising server reset limit, the experience rate by resets,
/// and the PvP spots with prestige. The cash shop is deactivated.
/// </summary>
public static class ClassicPlugInDefaults
{
    /// <summary>
    /// The maximum reset count which the server reset limit reaches.
    /// </summary>
    public const int MaximumResets = 55;

    /// <summary>
    /// Applies the defaults to the plugin configurations of the game configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    /// <param name="utcNow">The current point in time, from which the server reset limit starts to rise.</param>
    public static void Apply(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler, DateTime utcNow)
    {
        if (Find<ResetFeaturePlugIn>(gameConfiguration) is { } reset)
        {
            reset.IsActive = true;
            reset.SetConfiguration(CreateResetConfiguration(utcNow), referenceHandler);
        }

        if (Find<ResetExperienceRatePlugIn>(gameConfiguration) is { } experience)
        {
            experience.IsActive = true;
            experience.SetConfiguration(ResetExperienceRateConfiguration.CreateDefault(), referenceHandler);
        }

        if (Find<HuntingJewelPlugIn>(gameConfiguration) is { } huntingJewels)
        {
            huntingJewels.IsActive = true;
            huntingJewels.SetConfiguration(new HuntingJewelConfiguration(), referenceHandler);
        }

        if (Find<HuntingPointsPlugIn>(gameConfiguration) is { } huntingPoints)
        {
            huntingPoints.IsActive = true;
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
    /// Resets the boss lists of the <see cref="BossMapMarkersPlugIn"/> and the <see cref="BossStatusChatCommandPlugIn"/>
    /// to their defaults, which are only the field bosses: Balrog, Hydra, Zaikan, Death Beam Knight, Dark Phoenix and Hell Maine.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="referenceHandler">The reference handler.</param>
    public static void ResetBossLists(GameConfiguration gameConfiguration, ReferenceHandler referenceHandler)
    {
        Find<BossMapMarkersPlugIn>(gameConfiguration)?.SetConfiguration(new BossMapMarkersConfiguration(), referenceHandler);
        Find<BossStatusChatCommandPlugIn>(gameConfiguration)?.SetConfiguration(new BossStatusChatCommandPlugIn.BossStatusConfiguration(), referenceHandler);
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
    /// The character starts again at level 10. The server reset limit starts at 1 and rises by 1 per day up to <see cref="MaximumResets"/>.
    /// </summary>
    /// <param name="utcNow">The current point in time, from which the server reset limit starts to rise.</param>
    /// <returns>The reset configuration.</returns>
    public static ResetConfiguration CreateResetConfiguration(DateTime utcNow)
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
            MultiplyPointsByResetCount = false,
            PointsPerReset = 1000,
            ResetLimit = MaximumResets,
            ServerResetLimitStart = utcNow.Date,
            ServerResetLimitInitial = 1,
            ServerResetLimitIncreasePerDay = 1,
            MoveHome = true,
            LogOut = true,
        };

        // The early resets give extra points for a fast start.
        configuration.PointsTiers.Add(new ResetConfiguration.ResetPointTier { MinimumResetCount = 1, PointsGranted = 2000 });
        configuration.PointsTiers.Add(new ResetConfiguration.ResetPointTier { MinimumResetCount = 2, PointsGranted = 1500 });
        configuration.PointsTiers.Add(new ResetConfiguration.ResetPointTier { MinimumResetCount = 5, PointsGranted = 1000 });
        return configuration;
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

    private static PlugInConfiguration? Find<TPlugIn>(GameConfiguration gameConfiguration)
        => gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(TPlugIn).GUID);
}
