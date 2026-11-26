// <copyright file="ClassicIcarusMonsters.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Makes the strongest normal monsters of Icarus four times as strong as in the original data, because characters
/// with resets kill them too fast. Health, damage, defense, attack rate and defense rate are multiplied by four;
/// level, experience and drops stay unchanged.
/// </summary>
/// <remarks>
/// Drakan and Alpha Crust are also used in the Devil Square, where they get stronger as well.
/// </remarks>
public static class ClassicIcarusMonsters
{
    /// <summary>
    /// The factor by which the original stats were multiplied.
    /// </summary>
    public const int StrengthFactor = 4;

    /// <summary>
    /// Gets the strengthened monsters with their stats (the original stats times <see cref="StrengthFactor"/>).
    /// </summary>
    public static IReadOnlyList<(short MonsterNumber, string Name, ClassicBosses.BossStrength Strength)> Monsters { get; } =
    [
        (72, "Phantom Knight", new(164_000, 2_240, 2_440, 1_700, 2_760, 1_080)),
        (73, "Drakan", new(116_000, 1_700, 1_920, 1_220, 2_280, 840)),
        (74, "Alpha Crust", new(138_000, 1_956, 2_160, 1_440, 2_480, 960)),
        (75, "Great Drakan", new(200_000, 2_600, 2_800, 1_980, 3_200, 1_220)),
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
