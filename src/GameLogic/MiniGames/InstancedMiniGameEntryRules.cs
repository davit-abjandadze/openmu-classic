// <copyright file="InstancedMiniGameEntryRules.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

/// <summary>
/// The rules to enter an instanced mini game: which tier a level belongs to, and whether a solo player or a party may enter.
/// </summary>
/// <remarks>
/// A party enters together or not at all: when one member doesn't meet the requirements, nobody enters.
/// The party plays the tier of its lowest member, so a low level member never has to face stronger monsters.
/// </remarks>
public static class InstancedMiniGameEntryRules
{
    /// <summary>
    /// Selects the tier of the mini game for the level of a character.
    /// </summary>
    /// <param name="definitions">The definitions (tiers) of the mini game.</param>
    /// <param name="level">The level of the character.</param>
    /// <param name="isSpecialCharacter">If set to <c>true</c>, the character is a special character (e.g. a Magic Gladiator) which uses the special level ranges.</param>
    /// <returns>The tier which contains the level; the highest tier, if the level is above all tiers; or <c>null</c>, if it's below all tiers.</returns>
    public static MiniGameDefinition? SelectTier(IEnumerable<MiniGameDefinition> definitions, int level, bool isSpecialCharacter)
    {
        var tiers = definitions.OrderBy(d => d.GameLevel).ToList();
        int Minimum(MiniGameDefinition d) => isSpecialCharacter ? d.MinimumSpecialCharacterLevel : d.MinimumCharacterLevel;
        int Maximum(MiniGameDefinition d) => isSpecialCharacter ? d.MaximumSpecialCharacterLevel : d.MaximumCharacterLevel;

        return tiers.FirstOrDefault(d => level >= Minimum(d) && level <= Maximum(d))
               ?? tiers.LastOrDefault(d => level > Maximum(d));
    }

    /// <summary>
    /// Selects the tier of a party: the lowest tier of its members.
    /// </summary>
    /// <param name="candidates">The members of the party.</param>
    /// <returns>The tier; or <c>null</c>, if a member has no tier.</returns>
    public static MiniGameDefinition? SelectPartyTier(IReadOnlyCollection<InstancedEntryCandidate> candidates)
    {
        if (candidates.Count == 0 || candidates.Any(c => c.Tier is null))
        {
            return null;
        }

        return candidates.Select(c => c.Tier!).MinBy(t => t.GameLevel);
    }

    /// <summary>
    /// Validates the requirements of every member.
    /// </summary>
    /// <param name="candidates">The members of the party, or the solo player.</param>
    /// <returns>The members which can't enter, with the reason. When it isn't empty, nobody enters.</returns>
    public static IReadOnlyList<(string Name, InstancedEntryRefusal Reason)> Validate(IEnumerable<InstancedEntryCandidate> candidates)
    {
        return candidates
            .Select(c => (c.Name, Reason: GetRefusal(c)))
            .Where(r => r.Reason != InstancedEntryRefusal.None)
            .ToList();
    }

    /// <summary>
    /// Gets the reason why the candidate can't enter.
    /// </summary>
    /// <param name="candidate">The candidate.</param>
    /// <returns>The reason; or <see cref="InstancedEntryRefusal.None"/>, if it can enter.</returns>
    public static InstancedEntryRefusal GetRefusal(InstancedEntryCandidate candidate)
    {
        if (!candidate.IsOnline)
        {
            return InstancedEntryRefusal.Offline;
        }

        if (!candidate.IsAlive)
        {
            return InstancedEntryRefusal.Dead;
        }

        if (!candidate.IsNearLeader)
        {
            return InstancedEntryRefusal.TooFarAway;
        }

        if (candidate.IsInOtherEvent)
        {
            return InstancedEntryRefusal.InOtherEvent;
        }

        if (candidate.IsPlayerKiller)
        {
            return InstancedEntryRefusal.PlayerKiller;
        }

        if (candidate.Tier is null)
        {
            return InstancedEntryRefusal.LevelTooLow;
        }

        if (!candidate.IsDailyLimitTracked)
        {
            return InstancedEntryRefusal.NotTracked;
        }

        if (candidate.HasEnteredToday)
        {
            return InstancedEntryRefusal.AlreadyEnteredToday;
        }

        return InstancedEntryRefusal.None;
    }
}
