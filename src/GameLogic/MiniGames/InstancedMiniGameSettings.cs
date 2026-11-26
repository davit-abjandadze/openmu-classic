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
    private readonly HashSet<Guid> _memberIds;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstancedMiniGameSettings"/> class.
    /// </summary>
    /// <param name="slot">The slot to which the instance belongs.</param>
    /// <param name="memberIds">The ids of the characters which entered the instance. Only they may (re-)enter it.</param>
    /// <param name="scaling">The scaling by the number of players.</param>
    /// <param name="isShared">If set to <c>true</c>, all solo players and parties of the tier share the instance.</param>
    public InstancedMiniGameSettings(InstancedMiniGameSlot slot, IEnumerable<Guid> memberIds, PartySizeScaling scaling, bool isShared = false)
    {
        this.Slot = slot;
        this._memberIds = memberIds.ToHashSet();
        this.Scaling = scaling;
        this.IsShared = isShared;
    }

    /// <summary>
    /// Gets the slot to which the instance belongs.
    /// </summary>
    public InstancedMiniGameSlot Slot { get; }

    /// <summary>
    /// Gets a value indicating whether all solo players and parties of the tier share the instance.
    /// Its scaling is then determined by the number of its players when the game starts.
    /// </summary>
    public bool IsShared { get; }

    /// <summary>
    /// Gets the scaling by the number of players.
    /// </summary>
    public PartySizeScaling Scaling { get; internal set; }

    /// <summary>
    /// Gets a value indicating whether party members above the maximum level of the tier are weakened to it.
    /// </summary>
    public bool IsLevelSyncEnabled => this.Slot.Configuration.IsLevelSyncEnabled;

    /// <summary>
    /// Gets a snapshot of the ids of the characters which entered the instance.
    /// </summary>
    public IReadOnlyCollection<Guid> MemberIds
    {
        get
        {
            lock (this._memberIds)
            {
                return this._memberIds.ToList();
            }
        }
    }

    /// <summary>
    /// Determines whether the player is a member of the instance.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the player is a member; otherwise, <c>false</c>.</returns>
    public bool IsMember(Player player)
    {
        if (player.SelectedCharacter is not { } character)
        {
            return false;
        }

        lock (this._memberIds)
        {
            return this._memberIds.Contains(character.Id);
        }
    }

    /// <summary>
    /// Adds the characters which entered a shared instance.
    /// </summary>
    /// <param name="memberIds">The ids of the characters.</param>
    internal void AddMembers(IEnumerable<Guid> memberIds)
    {
        lock (this._memberIds)
        {
            this._memberIds.UnionWith(memberIds);
        }
    }
}
