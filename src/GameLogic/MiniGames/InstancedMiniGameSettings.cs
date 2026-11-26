// <copyright file="InstancedMiniGameSettings.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// The settings of one instance of an instanced mini game: to which slot it belongs, who may enter it,
/// and how it's scaled by the number of its players.
/// </summary>
public sealed class InstancedMiniGameSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InstancedMiniGameSettings"/> class.
    /// </summary>
    /// <param name="slot">The slot to which the instance belongs.</param>
    /// <param name="memberIds">The ids of the characters which entered the instance together. Only they may (re-)enter it.</param>
    /// <param name="scaling">The scaling by the number of players.</param>
    public InstancedMiniGameSettings(InstancedMiniGameSlot slot, IEnumerable<Guid> memberIds, PartySizeScaling scaling)
    {
        this.Slot = slot;
        this.MemberIds = memberIds.ToHashSet();
        this.Scaling = scaling;
    }

    /// <summary>
    /// Gets the slot to which the instance belongs.
    /// </summary>
    public InstancedMiniGameSlot Slot { get; }

    /// <summary>
    /// Gets the ids of the characters which entered the instance together. Only they may (re-)enter it.
    /// </summary>
    public IReadOnlySet<Guid> MemberIds { get; }

    /// <summary>
    /// Gets the scaling by the number of players.
    /// </summary>
    public PartySizeScaling Scaling { get; }

    /// <summary>
    /// Gets a value indicating whether party members above the maximum level of the tier are weakened to it.
    /// </summary>
    public bool IsLevelSyncEnabled => this.Slot.Configuration.IsLevelSyncEnabled;

    /// <summary>
    /// Determines whether the player is a member of the instance.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the player is a member; otherwise, <c>false</c>.</returns>
    public bool IsMember(Player player) => player.SelectedCharacter is { } character && this.MemberIds.Contains(character.Id);
}
