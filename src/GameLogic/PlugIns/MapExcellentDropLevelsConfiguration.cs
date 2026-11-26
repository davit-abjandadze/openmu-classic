// <copyright file="MapExcellentDropLevelsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="MapExcellentDropLevelsPlugIn"/>.
/// </summary>
public class MapExcellentDropLevelsConfiguration
{
    /// <summary>
    /// Gets or sets the drop levels of the excellent items per map. On maps which aren't listed, excellent items drop by the monster level.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.MapExcellentDropLevelsConfiguration_Maps_Name), Description = nameof(PlugInResources.MapExcellentDropLevelsConfiguration_Maps_Description))]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<MapDropLevels> Maps { get; set; } = [];

    /// <summary>
    /// The drop levels of the excellent items on a map.
    /// </summary>
    public class MapDropLevels
    {
        /// <summary>
        /// Gets or sets the number of the map.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.MapExcellentDropLevelsConfiguration_MapNumber_Name))]
        public short MapNumber { get; set; }

        /// <summary>
        /// Gets or sets the lowest drop level of the excellent items which drop on the map.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.MapExcellentDropLevelsConfiguration_MinimumDropLevel_Name), Description = nameof(PlugInResources.MapExcellentDropLevelsConfiguration_MinimumDropLevel_Description))]
        public int MinimumDropLevel { get; set; }

        /// <summary>
        /// Gets or sets the highest drop level of the excellent items which drop on the map.
        /// </summary>
        [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.MapExcellentDropLevelsConfiguration_MaximumDropLevel_Name), Description = nameof(PlugInResources.MapExcellentDropLevelsConfiguration_MaximumDropLevel_Description))]
        public int MaximumDropLevel { get; set; }
    }
}
