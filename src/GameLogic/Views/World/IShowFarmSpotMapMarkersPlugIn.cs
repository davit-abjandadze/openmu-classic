// <copyright file="IShowFarmSpotMapMarkersPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.World;

using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Interface of a view whose implementation shows the farm spots of a map as dots on the map of the client.
/// </summary>
/// <remarks>
/// Only the open source client has an implementation of it.
/// </remarks>
public interface IShowFarmSpotMapMarkersPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the farm spots of a map. They replace the previously shown farm spots of the same map.
    /// </summary>
    /// <param name="mapNumber">The number of the map.</param>
    /// <param name="spots">The centres of the farm spots of the map.</param>
    ValueTask ShowFarmSpotMapMarkersAsync(short mapNumber, IReadOnlyCollection<Point> spots);
}
