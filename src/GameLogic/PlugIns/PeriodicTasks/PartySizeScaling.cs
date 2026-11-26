// <copyright file="PartySizeScaling.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

/// <summary>
/// The scaling of an instanced mini game run by the number of players which entered it together,
/// so that a solo player can finish it while a full party doesn't find it too easy.
/// </summary>
public class PartySizeScaling
{
    /// <summary>
    /// Gets or sets the number of players to which this scaling applies.
    /// Bigger groups use the scaling with the highest player count which isn't bigger than their size.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.PartySizeScaling_PlayerCount_Caption))]
    public int PlayerCount { get; set; }

    /// <summary>
    /// Gets or sets the factor for the number of monsters which spawn during the run.
    /// Every spawn keeps at least one monster, so bosses are never scaled away.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.PartySizeScaling_MonsterCountFactor_Caption))]
    public double MonsterCountFactor { get; set; } = 1;

    /// <summary>
    /// Gets or sets the factor for the maximum health of the monsters of the run, including the bosses.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.PartySizeScaling_MonsterHealthFactor_Caption))]
    public double MonsterHealthFactor { get; set; } = 1;

    /// <summary>
    /// Gets or sets the factor for the experience and money rewards of every player of the run.
    /// Item rewards are not scaled.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.PartySizeScaling_RewardFactor_Caption))]
    public double RewardFactor { get; set; } = 1;

    /// <summary>
    /// Gets the scaling which doesn't change anything.
    /// </summary>
    internal static PartySizeScaling Neutral { get; } = new() { PlayerCount = 1 };

    /// <summary>
    /// Gets the scaling for the specified number of players.
    /// </summary>
    /// <param name="scalings">The configured scalings.</param>
    /// <param name="playerCount">The number of players.</param>
    /// <returns>
    /// The scaling with the highest <see cref="PlayerCount"/> which isn't bigger than <paramref name="playerCount"/>;
    /// the smallest one, if all are bigger; or <see cref="Neutral"/>, if none is configured.
    /// </returns>
    public static PartySizeScaling Find(IEnumerable<PartySizeScaling>? scalings, int playerCount)
    {
        var ordered = scalings?.Where(s => s is not null).OrderBy(s => s.PlayerCount).ToList() ?? [];
        if (ordered.Count == 0)
        {
            return Neutral;
        }

        return ordered.LastOrDefault(s => s.PlayerCount <= playerCount) ?? ordered[0];
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{this.PlayerCount} player(s): monsters x{this.MonsterCountFactor}, health x{this.MonsterHealthFactor}, rewards x{this.RewardFactor}";
    }
}
