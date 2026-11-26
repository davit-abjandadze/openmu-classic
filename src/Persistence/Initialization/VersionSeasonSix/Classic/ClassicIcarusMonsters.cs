// <copyright file="ClassicIcarusMonsters.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Makes the strongest normal monsters of Icarus stronger than in the original data, because characters
/// with resets kill them too fast. Damage, attack rate and defense rate are multiplied by four, health and
/// defense by two; level, experience and drops stay unchanged.
/// </summary>
/// <remarks>
/// Drakan and Alpha Crust are also used in the Devil Square, where they get stronger as well.
/// </remarks>
public static class ClassicIcarusMonsters
{
    /// <summary>
    /// The factor by which the original damage, attack rate and defense rate were multiplied.
    /// </summary>
    public const int StrengthFactor = 4;

    /// <summary>
    /// The factor by which the original health and defense were multiplied.
    /// </summary>
    public const int HealthAndDefenseFactor = 2;

    /// <summary>
    /// Gets the strengthened monsters with their stats (the original health and defense times <see cref="HealthAndDefenseFactor"/>,
    /// the other original stats times <see cref="StrengthFactor"/>).
    /// </summary>
    public static IReadOnlyList<(short MonsterNumber, string Name, ClassicBosses.BossStrength Strength)> Monsters { get; } =
    [
        (72, "Phantom Knight", new(82_000, 2_240, 2_440, 850, 2_760, 1_080)),
        (73, "Drakan", new(58_000, 1_700, 1_920, 610, 2_280, 840)),
        (74, "Alpha Crust", new(69_000, 1_956, 2_160, 720, 2_480, 960)),
        (75, "Great Drakan", new(100_000, 2_600, 2_800, 990, 3_200, 1_220)),
    ];

    /// <summary>
    /// Applies the stats to the monster definitions. It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var (number, _, strength) in Monsters)
        {
            if (gameConfiguration.Monsters.FirstOrDefault(m => m.Number == number) is { } monster)
            {
                ClassicBosses.ApplyStrength(context, gameConfiguration, monster, strength);
            }
        }
    }
}
