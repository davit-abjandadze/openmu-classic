// <copyright file="InstancedMiniGameStartConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using MUnique.OpenMU.Interfaces;

/// <summary>
/// The start configuration of a mini game which can run in instances: when its definitions use
/// <see cref="MiniGameMapCreationPolicy.OnePerParty"/>, every start of the <see cref="PeriodicTaskConfiguration.Timetable"/>
/// opens a time slot, in which every solo player or party enters its own instance by a chat command.
/// For definitions with <see cref="MiniGameMapCreationPolicy.Shared"/>, only the announcements before the start apply.
/// </summary>
public abstract class InstancedMiniGameStartConfiguration : MiniGameStartConfiguration
{
    /// <summary>
    /// The default maximum distance of a party member to its leader, when the leader enters with the party.
    /// </summary>
    internal const int DefaultMaximumPartyDistance = 15;

    /// <summary>
    /// The default maximum number of instances per slot.
    /// </summary>
    internal const int DefaultMaximumInstancesPerSlot = 100;

    /// <summary>
    /// The default duration in which the instances can be entered after the start of a slot.
    /// </summary>
    internal static readonly TimeSpan DefaultEntranceDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The default duration of the game of an instance, after its entrance closed.
    /// </summary>
    internal static readonly TimeSpan DefaultGameDuration = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets or sets the times before a start, at which the <see cref="UpcomingMessage"/> is announced.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_AnnouncementLeadTimes_Caption), Order = 110)]
    public IList<TimeSpan> AnnouncementLeadTimes { get; set; } = new List<TimeSpan>();

    /// <summary>
    /// Gets or sets the message which is announced before a start. The placeholder {0} is the number of minutes until the start.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_UpcomingMessage_Caption), Order = 111)]
    public LocalizedString UpcomingMessage { get; set; }

    /// <summary>
    /// Gets or sets the duration in which the instances can be entered after the start of a slot.
    /// The games of all instances of the slot start when it elapsed.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_EntranceDuration_Caption), Order = 112)]
    public TimeSpan EntranceDuration { get; set; } = DefaultEntranceDuration;

    /// <summary>
    /// Gets or sets the duration of the game of an instance, after its entrance closed.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_GameDuration_Caption), Order = 113)]
    public TimeSpan GameDuration { get; set; } = DefaultGameDuration;

    /// <summary>
    /// Gets or sets the maximum distance of a party member to its leader, when the leader enters with the party.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_MaximumPartyDistance_Caption), Order = 114)]
    public int MaximumPartyDistance { get; set; } = DefaultMaximumPartyDistance;

    /// <summary>
    /// Gets or sets the maximum number of instances per slot and game server, which protects the server from too many maps.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_MaximumInstancesPerSlot_Caption), Order = 115)]
    public int MaximumInstancesPerSlot { get; set; } = DefaultMaximumInstancesPerSlot;

    /// <summary>
    /// Gets or sets a value indicating whether all solo players and parties of a tier share one instance per slot.
    /// Otherwise, every solo player or party gets its own instance.
    /// A full shared instance is followed by another one of the same tier.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_IsSharedPerTier_Caption), Order = 118)]
    public bool IsSharedPerTier { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether party members above the maximum level of the entered tier are weakened to it (level sync).
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_IsLevelSyncEnabled_Caption), Order = 116)]
    public bool IsLevelSyncEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the scaling of the monsters and rewards by the number of players of an instance.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.InstancedMiniGameStartConfiguration_PartySizeScalings_Caption), Order = 117)]
    public IList<PartySizeScaling> PartySizeScalings { get; set; } = new List<PartySizeScaling>();

    /// <summary>
    /// Gets the entrance duration, falling back to the default for configurations which were stored before it existed.
    /// </summary>
    internal TimeSpan EffectiveEntranceDuration => this.EntranceDuration > TimeSpan.Zero ? this.EntranceDuration : DefaultEntranceDuration;

    /// <summary>
    /// Gets the game duration, falling back to the default for configurations which were stored before it existed.
    /// </summary>
    internal TimeSpan EffectiveGameDuration => this.GameDuration > TimeSpan.Zero ? this.GameDuration : DefaultGameDuration;

    /// <summary>
    /// Gets the maximum party distance, falling back to the default for configurations which were stored before it existed.
    /// </summary>
    internal int EffectiveMaximumPartyDistance => this.MaximumPartyDistance > 0 ? this.MaximumPartyDistance : DefaultMaximumPartyDistance;

    /// <summary>
    /// Gets the maximum number of instances per slot, falling back to the default for configurations which were stored before it existed.
    /// </summary>
    internal int EffectiveMaximumInstancesPerSlot => this.MaximumInstancesPerSlot > 0 ? this.MaximumInstancesPerSlot : DefaultMaximumInstancesPerSlot;

    /// <summary>
    /// Gets the default scalings by the number of players: fewer and weaker monsters, but more rewards for small groups.
    /// </summary>
    /// <returns>The default scalings.</returns>
    public static List<PartySizeScaling> CreateDefaultPartySizeScalings() =>
    [
        new() { PlayerCount = 1, MonsterCountFactor = 0.6, MonsterHealthFactor = 0.7, RewardFactor = 1.5 },
        new() { PlayerCount = 2, MonsterCountFactor = 0.8, MonsterHealthFactor = 0.85, RewardFactor = 1.2 },
        new() { PlayerCount = 3, MonsterCountFactor = 1.0, MonsterHealthFactor = 1.0, RewardFactor = 1.0 },
        new() { PlayerCount = 4, MonsterCountFactor = 1.15, MonsterHealthFactor = 1.1, RewardFactor = 0.9 },
        new() { PlayerCount = 5, MonsterCountFactor = 1.3, MonsterHealthFactor = 1.2, RewardFactor = 0.85 },
    ];
}
