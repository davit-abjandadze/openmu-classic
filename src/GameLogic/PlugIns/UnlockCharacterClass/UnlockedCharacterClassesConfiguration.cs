// <copyright file="UnlockedCharacterClassesConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;

/// <summary>
/// The configuration of the <see cref="UnlockedCharacterClassesPlugIn"/>.
/// </summary>
public class UnlockedCharacterClassesConfiguration
{
    /// <summary>
    /// The number of the Magic Gladiator class.
    /// </summary>
    public const byte MagicGladiatorNumber = 12;

    /// <summary>
    /// Gets or sets the numbers of the character classes which every account can create from the start.
    /// </summary>
    [Display(Name = "Character classes", Description = "The numbers of the character classes which every account can create from the start, e.g. 12 for the Magic Gladiator.")]
    public ICollection<byte> ClassNumbers { get; set; } = new List<byte> { MagicGladiatorNumber };
}
