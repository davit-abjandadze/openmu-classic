// <copyright file="HuntingPointsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.HuntingRewards;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="HuntingPointsPlugIn"/>.
/// </summary>
public class HuntingPointsConfiguration
{
    /// <summary>
    /// Gets or sets the minimum character level from which hunting points are earned.
    /// </summary>
    [Display(Name = "Minimum level")]
    public int MinimumLevel { get; set; } = 400;

    /// <summary>
    /// Gets or sets the hunting time for one point.
    /// </summary>
    [Display(Name = "Hunting time per point")]
    public TimeSpan HuntingTimePerPoint { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Gets or sets the maximum hunting time per day which counts for points.
    /// </summary>
    [Display(Name = "Maximum hunting time per day")]
    public TimeSpan MaximumHuntingTimePerDay { get; set; } = TimeSpan.FromHours(4);

    /// <summary>
    /// Gets or sets the maximum time between two kills which counts as hunting time.
    /// </summary>
    [Display(Name = "Maximum time between kills", Description = "The time between two monster kills counts as hunting time, but at most this time. So idle time doesn't count.")]
    public TimeSpan MaximumTimeBetweenKills { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the offers, for which hunting points can be exchanged with the /hunt command.
    /// </summary>
    [Display(Name = "Exchange offers")]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<HuntingPointOffer> Offers { get; set; } = [];

    /// <summary>
    /// Creates the default configuration with offers for jewels and Boxes of Kundun.
    /// </summary>
    /// <returns>The default configuration.</returns>
    public static HuntingPointsConfiguration CreateDefault()
    {
        var configuration = new HuntingPointsConfiguration();
        configuration.Offers.Add(new HuntingPointOffer { Key = "chaos", Points = 15, ItemGroup = 12, ItemNumber = 15 });
        configuration.Offers.Add(new HuntingPointOffer { Key = "bless", Points = 30, ItemGroup = 14, ItemNumber = 13 });
        configuration.Offers.Add(new HuntingPointOffer { Key = "soul", Points = 30, ItemGroup = 14, ItemNumber = 14 });
        configuration.Offers.Add(new HuntingPointOffer { Key = "life", Points = 45, ItemGroup = 14, ItemNumber = 16 });
        configuration.Offers.Add(new HuntingPointOffer { Key = "creation", Points = 45, ItemGroup = 14, ItemNumber = 22 });
        configuration.Offers.Add(new HuntingPointOffer { Key = "box1", Name = "Box of Kundun +1", Points = 60, ItemGroup = 14, ItemNumber = 11, ItemLevel = 8 });
        configuration.Offers.Add(new HuntingPointOffer { Key = "box2", Name = "Box of Kundun +2", Points = 120, ItemGroup = 14, ItemNumber = 11, ItemLevel = 9 });
        return configuration;
    }

    /// <summary>
    /// An offer, for which hunting points can be exchanged.
    /// </summary>
    public class HuntingPointOffer
    {
        /// <summary>
        /// Gets or sets the key, with which the offer is selected, e.g. "/hunt bless".
        /// </summary>
        [Display(Name = "Key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name which is shown. When it's empty, the name of the item with its level is shown.
        /// </summary>
        [Display(Name = "Name", Description = "The shown name. When it's empty, the name of the item with its level is shown.")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the required hunting points.
        /// </summary>
        [Display(Name = "Points")]
        public int Points { get; set; }

        /// <summary>
        /// Gets or sets the group of the item.
        /// </summary>
        [Display(Name = "Item group")]
        public byte ItemGroup { get; set; }

        /// <summary>
        /// Gets or sets the number of the item.
        /// </summary>
        [Display(Name = "Item number")]
        public short ItemNumber { get; set; }

        /// <summary>
        /// Gets or sets the level of the item.
        /// </summary>
        [Display(Name = "Item level")]
        public byte ItemLevel { get; set; }
    }
}
