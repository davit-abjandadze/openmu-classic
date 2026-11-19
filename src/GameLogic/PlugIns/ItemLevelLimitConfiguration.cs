// <copyright file="ItemLevelLimitConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The configuration of the <see cref="ItemLevelLimitPlugIn"/>.
/// </summary>
public class ItemLevelLimitConfiguration
{
    /// <summary>
    /// The default maximum item level.
    /// </summary>
    public const byte DefaultMaximumItemLevel = 11;

    /// <summary>
    /// Gets or sets the maximum item level which can be reached by upgrading.
    /// </summary>
    [Display(Name = "Maximum item level", Description = "The highest item level (+) which can be reached by upgrading in the chaos machine.")]
    public byte MaximumItemLevel { get; set; } = DefaultMaximumItemLevel;
}
