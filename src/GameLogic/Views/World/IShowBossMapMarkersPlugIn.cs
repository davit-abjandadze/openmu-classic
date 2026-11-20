// <copyright file="IShowBossMapMarkersPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.World;

using MUnique.OpenMU.Pathfinding;

/// <summary>
/// Interface of a view whose implementation shows the bosses of a map on the map of the client,
/// with the remaining time until a killed boss respawns.
/// </summary>
/// <remarks>
/// Only the open source client has an implementation of it.
/// </remarks>
public interface IShowBossMapMarkersPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the bosses of a map. They replace the previously shown bosses of the same map.
    /// </summary>
    /// <param name="mapNumber">The number of the map.</param>
    /// <param name="bosses">The bosses of the map.</param>
    ValueTask ShowBossMapMarkersAsync(short mapNumber, IReadOnlyCollection<BossMapMarker> bosses);
}

/// <summary>
/// A boss on the map.
/// </summary>
/// <param name="Name">The name of the boss.</param>
/// <param name="Position">The position of the boss, or the place of its death when it's not alive.</param>
/// <param name="IsAlive">If set to <c>true</c>, the boss is alive.</param>
/// <param name="RespawnIn">The remaining time until the boss respawns, when it's not alive.</param>
public sealed record BossMapMarker(string Name, Point Position, bool IsAlive, TimeSpan RespawnIn);
