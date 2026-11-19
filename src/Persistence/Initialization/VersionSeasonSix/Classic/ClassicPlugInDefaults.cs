// <copyright file="ClassicPlugInDefaults.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.HuntingRewards;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Activates and configures the plugins of the classic server:
/// resets with a rising required level and a rising server reset limit, the experience rate by resets,
/// and the PvP spots with prestige.
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

    private static PlugInConfiguration? Find<TPlugIn>(GameConfiguration gameConfiguration)
        => gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(TPlugIn).GUID);
}
