// <copyright file="UnlockedCharacterClassesPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.UnlockCharacterClass;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Unlocks the configured character classes for every account, so they can be created without a level requirement,
/// e.g. the Magic Gladiator.
/// </summary>
/// <remarks>
/// The client enables the creation button of such a class only if it's unlocked for the account.
/// The class is added to the <see cref="Account.UnlockedCharacterClasses"/> before the character list is shown.
/// Only active classes which can get created are unlocked.
/// </remarks>
[PlugIn]
[Display(Name = "Unlocked character classes", Description = "Every account can create the configured character classes from the start, e.g. the Magic Gladiator without a character of level 220.")]
[Guid("9C4B7E21-6A3F-4D58-B2E9-1F7C0A8D3E64")]
public class UnlockedCharacterClassesPlugIn : ICharacterListRequestingPlugIn, ISupportCustomConfiguration<UnlockedCharacterClassesConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public UnlockedCharacterClassesConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public void CharacterListRequesting(Player player)
    {
        // Bots never create characters, see UnlockCharacterAtLevelBase.
        if (this.Configuration is not { } configuration
            || player.Account is not { IsBot: false } account)
        {
            return;
        }

        foreach (var characterClass in player.GameContext.Configuration.CharacterClasses)
        {
            if (configuration.ClassNumbers.Contains(characterClass.Number)
                && characterClass is { IsActive: true, CanGetCreated: true }
                && account.UnlockedCharacterClasses.All(c => c.Number != characterClass.Number))
            {
                account.UnlockedCharacterClasses.Add(characterClass);
            }
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new UnlockedCharacterClassesConfiguration();
}
