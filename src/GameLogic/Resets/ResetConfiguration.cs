// <copyright file="ResetConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// Configuration of the Reset System.
/// </summary>
public class ResetConfiguration
{
    /// <summary>
    /// Gets or sets the reset limit, which is the maximum amount of possible resets.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_ResetLimit_Name))]
    public int? ResetLimit { get; set; }

    /// <summary>
    /// Gets or sets the required level for a reset.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_RequiredLevel_Name))]
    public int RequiredLevel { get; set; } = 400;

    /// <summary>
    /// Gets or sets the character level after a reset.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_LevelAfterReset_Name))]
    public int LevelAfterReset { get; set; } = 10;

    /// <summary>
    /// Gets or sets the required money for a reset.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_RequiredMoney_Name))]
    public int RequiredMoney { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether the required money should
    /// be multiplied with the current reset count.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_MultiplyRequiredMoneyByResetCount_Name))]
    public bool MultiplyRequiredMoneyByResetCount { get; set; } = true;

    /// <summary>
    /// Gets or sets the required item for a reset.
    /// </summary>
    [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.ResetConfiguration_RequiredResetItem_Name), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
    public ItemDefinition? RequiredResetItem { get; set; }

    /// <summary>
    /// Gets or sets the item costs per reset range.
    /// </summary>
    [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.ResetConfiguration_ItemCostTiers_Name), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<ResetItemCostTier> ItemCostTiers { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether a reset sets the stat points back to the initial values.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_ResetStats_Name))]
    public bool ResetStats { get; set; } = true;

    /// <summary>
    /// Gets or sets the legacy amount of points which will be set at the <see cref="Character.LevelUpPoints"/> when doing a reset.
    /// Use <see cref="PointsTiers"/> instead.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_PointsPerReset_Name))]
    [ScaffoldColumn(false)]
    public int PointsPerReset { get; set; } = 1500;

    /// <summary>
    /// Gets or sets a value indicating whether the legacy <see cref="PointsPerReset"/> should be multiplied with the current reset count.
    /// Use <see cref="PointsTiers"/> instead.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_MultiplyPointsByResetCount_Name))]
    [ScaffoldColumn(false)]
    public bool MultiplyPointsByResetCount { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a reset will replace (true) or add (false) the <see cref="Character.LevelUpPoints"/>.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_ReplacePointsPerReset_Name))]
    public bool ReplacePointsPerReset { get; set; } = true;

    /// <summary>
    /// Gets or sets the points granted per reset range.
    /// </summary>
    [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.ResetConfiguration_PointsTiers_Name), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<ResetPointTier> PointsTiers { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether a reset moves the player home.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_MoveHome_Name))]
    public bool MoveHome { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether a reset logs the player out back to character selection.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.ResetConfiguration_LogOut_Name))]
    public bool LogOut { get; set; } = true;

    /// <summary>
    /// Gets or sets the amount of levels which the required level increases with each reset which was done before.
    /// E.g. with a <see cref="RequiredLevel"/> of 350 and an increase of 10, the second reset requires level 360.
    /// </summary>
    [Display(Name = "Required level increase per reset", Description = "The required level increases by this amount for each reset which was done before.")]
    public int RequiredLevelIncreasePerReset { get; set; }

    /// <summary>
    /// Gets or sets the maximum required level, up to which the required level increases.
    /// When it's <c>null</c> or 0, the required level is not limited.
    /// </summary>
    [Display(Name = "Maximum required level", Description = "The required level doesn't increase above this level. Empty or 0 means no limit.")]
    public int? MaximumRequiredLevel { get; set; }

    /// <summary>
    /// Gets or sets the point in time (UTC) at which the server reset limit starts to rise.
    /// When it's <c>null</c>, there is no server reset limit, only the <see cref="ResetLimit"/>.
    /// </summary>
    /// <remarks>
    /// The server reset limit is the same for every character: it starts with <see cref="ServerResetLimitInitial"/>
    /// and rises by <see cref="ServerResetLimitIncreasePerDay"/> every 24 hours after this point in time,
    /// up to the <see cref="ResetLimit"/>, if one is defined.
    /// </remarks>
    [Display(Name = "Server reset limit start (UTC)", Description = "The point in time from which the reset limit of the whole server rises every 24 hours. Empty means no rising server limit.")]
    public DateTime? ServerResetLimitStart { get; set; }

    /// <summary>
    /// Gets or sets the server reset limit at <see cref="ServerResetLimitStart"/>.
    /// </summary>
    [Display(Name = "Server reset limit at start", Description = "The reset limit of the server on the first day.")]
    public int ServerResetLimitInitial { get; set; } = 1;

    /// <summary>
    /// Gets or sets the amount by which the server reset limit rises every 24 hours after <see cref="ServerResetLimitStart"/>.
    /// </summary>
    [Display(Name = "Server reset limit increase per day", Description = "The amount by which the reset limit of the server rises each day.")]
    public int ServerResetLimitIncreasePerDay { get; set; } = 1;

    /// <summary>
    /// Gets the required character level for the next reset.
    /// </summary>
    /// <param name="currentResetCount">The current reset count of the character.</param>
    /// <returns>The required character level.</returns>
    public int GetRequiredLevel(int currentResetCount)
    {
        var requiredLevel = (long)this.RequiredLevel + ((long)Math.Max(0, this.RequiredLevelIncreasePerReset) * Math.Max(0, currentResetCount));
        if (this.MaximumRequiredLevel is > 0 and var maximum)
        {
            requiredLevel = Math.Min(requiredLevel, maximum);
        }

        return (int)Math.Min(requiredLevel, int.MaxValue);
    }

    /// <summary>
    /// Gets the effective reset limit at the specified point in time, which considers
    /// the <see cref="ResetLimit"/> and the rising server reset limit.
    /// </summary>
    /// <param name="utcNow">The current point in time (UTC).</param>
    /// <returns>The effective reset limit, or <c>null</c> if there is no limit.</returns>
    public int? GetEffectiveResetLimit(DateTime utcNow)
    {
        int? limit = this.ResetLimit is > 0 ? this.ResetLimit : null;
        if (this.ServerResetLimitStart is not { } start)
        {
            return limit;
        }

        start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        var days = utcNow < start ? 0 : (long)(utcNow - start).TotalDays;
        var serverLimit = (int)Math.Clamp(this.ServerResetLimitInitial + (days * this.ServerResetLimitIncreasePerDay), 0, int.MaxValue);
        return limit is { } l ? Math.Min(l, serverLimit) : serverLimit;
    }

    /// <summary>
    /// Gets the point in time (UTC) when the server reset limit rises next, if it rises.
    /// </summary>
    /// <param name="utcNow">The current point in time (UTC).</param>
    /// <returns>The point in time of the next rise, or <c>null</c> if it doesn't rise anymore.</returns>
    public DateTime? GetNextServerResetLimitRise(DateTime utcNow)
    {
        if (this.ServerResetLimitStart is not { } start || this.ServerResetLimitIncreasePerDay <= 0)
        {
            return null;
        }

        if (this.ResetLimit is > 0 && this.GetEffectiveResetLimit(utcNow) >= this.ResetLimit)
        {
            return null;
        }

        start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (utcNow < start)
        {
            return start.AddDays(1);
        }

        var days = (long)(utcNow - start).TotalDays;
        return start.AddDays(days + 1);
    }

    /// <summary>
    /// Tier definition for the points which are granted when resetting.
    /// </summary>
    public class ResetPointTier
    {
        /// <summary>
        /// Gets or sets the minimum reset count at which this tier applies.
        /// </summary>
        [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.ResetPointTier_MinimumResetCount_Name), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
        public int MinimumResetCount { get; set; }

        /// <summary>
        /// Gets or sets the points granted for this tier.
        /// </summary>
        [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.ResetPointTier_PointsGranted_Name), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
        public int PointsGranted { get; set; }
    }

    /// <summary>
    /// Tier definition for required reset item amounts.
    /// </summary>
    public class ResetItemCostTier
    {
        /// <summary>
        /// Gets or sets the minimum reset count at which this tier applies.
        /// </summary>
        [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.ResetItemCostTier_MinimumResetCount_Name), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
        public int MinimumResetCount { get; set; }

        /// <summary>
        /// Gets or sets the required item amount for this tier.
        /// </summary>
        [Display(Name = nameof(MUnique.OpenMU.GameLogic.Properties.PlugInResources.ResetItemCostTier_RequiredItemAmount_Name), ResourceType = typeof(MUnique.OpenMU.GameLogic.Properties.PlugInResources))]
        public int RequiredItemAmount { get; set; }
    }
}
