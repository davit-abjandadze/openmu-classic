// <copyright file="ResetExperienceRateConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="ResetExperienceRatePlugIn"/>.
/// </summary>
public class ResetExperienceRateConfiguration
{
    /// <summary>
    /// The default tiers as (minimum reset count, multiplier relative to the configured experience rate).
    /// </summary>
    private static readonly (int Resets, float Multiplier)[] DefaultTiers =
    [
        (0, 1f), (5, 0.8f), (10, 0.6f), (15, 0.5f), (20, 0.4f), (25, 0.35f),
        (30, 0.3f), (35, 0.28f), (40, 0.27f), (45, 0.23f), (50, 0.2f),
    ];

    /// <summary>
    /// Gets or sets the tiers. The tier with the highest <see cref="ExperienceRateTier.MinimumResetCount"/>
    /// which is not above the reset count of a character applies.
    /// </summary>
    [Display(Name = "Tiers", Description = "The experience multiplier per reset count. The tier with the highest minimum reset count which the character reached applies.")]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<ExperienceRateTier> Tiers { get; set; } = [];

    /// <summary>
    /// Gets or sets the multiplier which applies when no tier matches.
    /// </summary>
    [Display(Name = "Default multiplier", Description = "The multiplier which applies when no tier matches.")]
    public float DefaultMultiplier { get; set; } = 1;

    /// <summary>
    /// Creates the default configuration: the full experience rate for the first resets, decreasing with each further reset.
    /// </summary>
    /// <remarks>
    /// The multipliers are relative to the configured experience rate of the game and the game server,
    /// because they are applied on top of it. The curve matches effective rates of 540, 432, 324, … 108
    /// for an experience rate of 540.
    /// </remarks>
    /// <returns>The default configuration.</returns>
    public static ResetExperienceRateConfiguration CreateDefault()
    {
        var configuration = new ResetExperienceRateConfiguration();
        foreach (var (resets, multiplier) in DefaultTiers)
        {
            configuration.Tiers.Add(new ExperienceRateTier { MinimumResetCount = resets, Multiplier = multiplier });
        }

        return configuration;
    }

    /// <summary>
    /// Gets the multiplier for the specified reset count.
    /// </summary>
    /// <param name="resetCount">The reset count of the character.</param>
    /// <returns>The multiplier.</returns>
    public float GetMultiplier(int resetCount)
    {
        return this.Tiers
            .Where(tier => tier.MinimumResetCount <= resetCount)
            .OrderByDescending(tier => tier.MinimumResetCount)
            .FirstOrDefault()?.Multiplier ?? this.DefaultMultiplier;
    }

    /// <summary>
    /// The experience multiplier from a minimum reset count on.
    /// </summary>
    public class ExperienceRateTier
    {
        /// <summary>
        /// Gets or sets the minimum reset count from which this tier applies.
        /// </summary>
        [Display(Name = "Minimum reset count")]
        public int MinimumResetCount { get; set; }

        /// <summary>
        /// Gets or sets the experience multiplier.
        /// </summary>
        [Display(Name = "Multiplier")]
        public float Multiplier { get; set; } = 1;
    }
}
