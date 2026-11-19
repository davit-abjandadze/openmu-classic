// <copyright file="ItemLevelLimitPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Limits the item level which can be reached by upgrading items in the chaos machine (e.g. the +10 to +15 mixes).
/// </summary>
/// <remarks>
/// The upgrade mixes stay in the data, so the limit can be raised at runtime without changing items or craftings.
/// Items which already have a higher level keep it.
/// </remarks>
[PlugIn]
[Display(Name = "Maximum item level", Description = "Limits the item level which can be reached by upgrading items in the chaos machine, e.g. to +11.")]
[Guid("C4F1A2B7-3E58-4D96-A0B1-6F2E9D7C5A38")]
public class ItemLevelLimitPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ItemLevelLimitConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public ItemLevelLimitConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the maximum item level which can be reached by upgrading, if the plugin is active.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The maximum item level, or <see cref="byte.MaxValue"/> when there is no limit.</returns>
    public static byte GetMaximumItemLevel(IGameContext gameContext)
        => gameContext.FeaturePlugIns.GetPlugIn<ItemLevelLimitPlugIn>()?.Configuration?.MaximumItemLevel ?? byte.MaxValue;

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ItemLevelLimitConfiguration();
}
