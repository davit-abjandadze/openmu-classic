// <copyright file="TeamVsTeamStats.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.TeamVsTeam;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// The attributes of a character which hold its results of the <see cref="TeamVsTeamPlugIn"/>.
/// To be persisted, they need to be stat attributes of the character classes.
/// </summary>
public static class TeamVsTeamStats
{
    /// <summary>
    /// Gets the points of won Team vs Team events (e.g. 30 for the best player of the winning team, 10 for the others).
    /// </summary>
    public static AttributeDefinition Points { get; } = new(new Guid("D4C7A1E3-2B5F-4A86-9C30-6E8F1B2D7A01"), "TvT Points", "The points of won Team vs Team events.");

    /// <summary>
    /// Gets the number of won Team vs Team events.
    /// </summary>
    public static AttributeDefinition Wins { get; } = new(new Guid("D4C7A1E3-2B5F-4A86-9C30-6E8F1B2D7A02"), "TvT Wins", "The number of won Team vs Team events.");

    /// <summary>
    /// Gets the number of times the character was the best player (MVP) of the winning team.
    /// </summary>
    public static AttributeDefinition MostValuablePlayer { get; } = new(new Guid("D4C7A1E3-2B5F-4A86-9C30-6E8F1B2D7A03"), "TvT MVP", "The number of times the character was the best player of the winning Team vs Team team.");

    /// <summary>
    /// Gets all attributes.
    /// </summary>
    public static IReadOnlyList<AttributeDefinition> All { get; } = [Points, Wins, MostValuablePlayer];
}
