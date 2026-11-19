// <copyright file="ResetExperienceRatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Multiplies the experience of a character by a factor which depends on its reset count,
/// e.g. high for the first resets and lower with each further reset.
/// </summary>
[PlugIn]
[Display(Name = "Experience rate by resets", Description = "Multiplies the experience of a character by a factor which depends on its reset count.")]
[Guid("C1F3A3B2-7D3E-4C55-9C1B-6A7A2E9D5B11")]
public class ResetExperienceRatePlugIn : IExperienceCalculationPlugIn, ISupportCustomConfiguration<ResetExperienceRateConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public ResetExperienceRateConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the experience multiplier of the player, if the plugin is active.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The multiplier, or <c>null</c> if the plugin is not active.</returns>
    public static float? GetMultiplierOf(Player player)
    {
        if (player.GameContext.PlugInManager.GetActivePlugInsOf<IExperienceCalculationPlugIn>().OfType<ResetExperienceRatePlugIn>().FirstOrDefault() is not { } plugIn
            || player.Attributes is not { } attributes)
        {
            return null;
        }

        return (plugIn.Configuration ?? new ResetExperienceRateConfiguration()).GetMultiplier((int)attributes[Stats.Resets]);
    }

    /// <inheritdoc />
    public ValueTask CalculateExperienceAsync(Player player, ExperienceCalculationArgs args)
    {
        if (args.IsMasterExperience || player.Attributes is not { } attributes)
        {
            return ValueTask.CompletedTask;
        }

        var configuration = this.Configuration ??= new ResetExperienceRateConfiguration();
        args.Experience *= configuration.GetMultiplier((int)attributes[Stats.Resets]);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => ResetExperienceRateConfiguration.CreateDefault();
}
