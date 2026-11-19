// <copyright file="ResetInfoChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which shows reset costs and rewards.
/// </summary>
[Guid("79F2C2C2-2E4C-4F4B-8A74-4227D1209D27")]
[PlugIn]
[Display(Name = nameof(PlugInResources.ResetInfoChatCommandPlugIn_Name), Description = nameof(PlugInResources.ResetInfoChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command)]
public class ResetInfoChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/resetinfo";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        var configuration = player.GameContext.FeaturePlugIns.GetPlugIn<ResetFeaturePlugIn>()?.Configuration;
        if (configuration is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ResetSystemInactive)).ConfigureAwait(false);
            return;
        }

        if (player.Attributes is null || player.SelectedCharacter is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnteredTheGame)).ConfigureAwait(false);
            return;
        }

        var progression = ResetProgressionCalculator.Calculate(
            (int)player.Attributes[Stats.Resets],
            (int)player.Attributes[Stats.PointsPerReset],
            configuration);
        var requiredLevel = configuration.GetRequiredLevel((int)player.Attributes[Stats.Resets]);
        await ShowServerLimitAndExperienceRateAsync(player, configuration).ConfigureAwait(false);

        if (progression.RequiredItemAmount > 0 && configuration.RequiredResetItem is { Name: { } itemName })
        {
            await player.ShowLocalizedBlueMessageAsync(
                    nameof(PlayerMessage.NextResetInfoCompactWithItem),
                    progression.NextResetCount,
                    requiredLevel,
                    progression.RequiredZen,
                    itemName,
                    progression.RequiredItemAmount,
                    progression.PointsForReset,
                    progression.TotalPointsAfterReset)
                .ConfigureAwait(false);
            return;
        }

        await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.NextResetInfoCompactNoItem),
                progression.NextResetCount,
                requiredLevel,
                progression.RequiredZen,
                progression.PointsForReset,
                progression.TotalPointsAfterReset)
            .ConfigureAwait(false);
    }

    private static async ValueTask ShowServerLimitAndExperienceRateAsync(Player player, ResetConfiguration configuration)
    {
        var now = DateTime.UtcNow;
        if (configuration.ServerResetLimitStart is not null && configuration.GetEffectiveResetLimit(now) is { } limit)
        {
            if (configuration.GetNextServerResetLimitRise(now) is { } nextRise
                && configuration.GetEffectiveResetLimit(nextRise) is { } nextLimit)
            {
                var remaining = nextRise - now;
                await player.ShowLocalizedBlueMessageAsync(
                        nameof(PlayerMessage.ResetInfoServerLimitRising),
                        limit,
                        nextLimit,
                        (int)remaining.TotalHours,
                        remaining.Minutes)
                    .ConfigureAwait(false);
            }
            else
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ResetInfoServerLimit), limit).ConfigureAwait(false);
            }
        }

        if (ResetExperienceRatePlugIn.GetMultiplierOf(player) is { } multiplier)
        {
            await player.ShowLocalizedBlueMessageAsync(
                    nameof(PlayerMessage.ResetInfoExperienceRate),
                    (multiplier * player.GameContext.ExperienceRate).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture))
                .ConfigureAwait(false);
        }
    }
}
