// <copyright file="PartySoloExperiencePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Gives every party member near the killer the experience which it would gain if it had killed the monster alone,
/// instead of a share of the party experience.
/// </summary>
/// <remarks>
/// <para>
/// Without this plugin, the experience of a kill is calculated for the average level of the members, raised by a party
/// bonus and split between them by their level. With it, each member gets the experience of a solo kill for its own level
/// and its own experience rates, including the rates of the <see cref="IExperienceCalculationPlugIn"/>s, e.g. by resets.
/// So hunting in a party gives each member as much experience per kill as hunting alone, and there is no party bonus.
/// </para>
/// <para>
/// Only the members which the killer sees get experience, as without this plugin. The money of a drop is still shared
/// by the experience which each member gained.
/// </para>
/// </remarks>
[PlugIn]
[Display(Name = nameof(PlugInResources.PartySoloExperiencePlugIn_Name), Description = nameof(PlugInResources.PartySoloExperiencePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("482E5368-78C0-4B5D-BBD8-A66F5107AFFB")]
public class PartySoloExperiencePlugIn : IFeaturePlugIn, IDisabledByDefault
{
    /// <summary>
    /// Determines whether the party members gain the experience of a solo kill.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns><c>true</c>, if the plugin is active; otherwise, <c>false</c>.</returns>
    public static bool IsActive(IGameContext gameContext)
        => gameContext.FeaturePlugIns.GetPlugIn<PartySoloExperiencePlugIn>() is not null;
}
