// <copyright file="InstancedMiniGameSlot.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using System.Threading;
using MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;
using Nito.AsyncEx;

/// <summary>
/// A time slot of an instanced mini game, which is opened by a start of its timetable.
/// While its entrance is open, every solo player or party enters its own instance.
/// The games of all instances of the slot start when the entrance closes.
/// </summary>
public sealed class InstancedMiniGameSlot
{
    private int _instanceCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="InstancedMiniGameSlot"/> class.
    /// </summary>
    /// <param name="type">The type of the mini game.</param>
    /// <param name="configuration">The configuration of the mini game start plugin.</param>
    /// <param name="startUtc">The start of the slot, in UTC.</param>
    public InstancedMiniGameSlot(MiniGameType type, InstancedMiniGameStartConfiguration configuration, DateTime startUtc)
    {
        this.Type = type;
        this.Configuration = configuration;
        this.StartUtc = startUtc;
        this.EntranceClosesUtc = startUtc.Add(configuration.EffectiveEntranceDuration);
        this.GameDuration = configuration.EffectiveGameDuration;
    }

    /// <summary>
    /// Gets the type of the mini game.
    /// </summary>
    public MiniGameType Type { get; }

    /// <summary>
    /// Gets the configuration of the mini game start plugin, at the start of the slot.
    /// </summary>
    public InstancedMiniGameStartConfiguration Configuration { get; }

    /// <summary>
    /// Gets the start of the slot, in UTC.
    /// </summary>
    public DateTime StartUtc { get; }

    /// <summary>
    /// Gets the time when the entrance closes and the games of the instances start, in UTC.
    /// </summary>
    public DateTime EntranceClosesUtc { get; }

    /// <summary>
    /// Gets the duration of the game of each instance.
    /// </summary>
    public TimeSpan GameDuration { get; }

    /// <summary>
    /// Gets the number of instances which were created in this slot.
    /// </summary>
    public int InstanceCount => Volatile.Read(ref this._instanceCount);

    /// <summary>
    /// Gets the lock which serializes the entries into the slot, so that two parties of the same tier
    /// don't create two shared instances at the same time.
    /// </summary>
    internal AsyncLock EntryLock { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the closing of the entrance has been announced.
    /// </summary>
    internal bool IsClosingAnnounced { get; set; }

    /// <summary>
    /// Determines whether the entrance of the slot is open at the specified point in time.
    /// </summary>
    /// <param name="utcNow">The point in time, in UTC.</param>
    /// <returns><c>true</c>, if the entrance is open; otherwise, <c>false</c>.</returns>
    public bool IsEntranceOpen(DateTime utcNow) => utcNow >= this.StartUtc && utcNow < this.EntranceClosesUtc;

    /// <summary>
    /// Tries to reserve one more instance, considering the configured maximum.
    /// </summary>
    /// <returns><c>true</c>, if an instance may be created; otherwise, <c>false</c>.</returns>
    internal bool TryReserveInstance()
    {
        var maximum = this.Configuration.EffectiveMaximumInstancesPerSlot;
        while (true)
        {
            var current = Volatile.Read(ref this._instanceCount);
            if (current >= maximum)
            {
                return false;
            }

            if (Interlocked.CompareExchange(ref this._instanceCount, current + 1, current) == current)
            {
                return true;
            }
        }
    }

    /// <summary>
    /// Releases a reserved instance which wasn't created in the end.
    /// </summary>
    internal void ReleaseInstance()
    {
        Interlocked.Decrement(ref this._instanceCount);
    }
}
