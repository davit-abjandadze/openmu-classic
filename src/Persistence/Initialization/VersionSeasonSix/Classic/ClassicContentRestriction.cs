// <copyright file="ClassicContentRestriction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;

/// <summary>
/// Restricts the content of the season 6 data to a classic server:
/// <list type="bullet">
///   <item>Only Dark Wizard, Dark Knight and Fairy Elf (and their second classes) are active and can be created.</item>
///   <item>Maps, monsters, items and mini games which were introduced after <see cref="TargetVersion"/> are deactivated.</item>
///   <item>Items can't be upgraded above <see cref="MaximumItemLevel"/>.</item>
///   <item>Skills, and the scrolls and orbs which teach them, have no character level requirement.</item>
///   <item>The buffs of Dark Wizard, Dark Knight and Fairy Elf only affect the caster, see <see cref="SelfOnlyBuffSkills"/>.</item>
/// </list>
/// </summary>
/// <remarks>
/// The content is only deactivated, not deleted, so it can be activated again in the admin panel (Content Activation).
/// Content with an unknown <see cref="GameVersion"/> stays active.
/// </remarks>
public class ClassicContentRestriction
{
    /// <summary>
    /// The newest game version of which the content stays active.
    /// </summary>
    public const GameVersion TargetVersion = GameVersion.Season1;

    /// <summary>
    /// The highest item level (+13) which the data supports.
    /// The upgrade mixes up to this level stay in the data; the actually reachable level is limited
    /// by the <see cref="GameLogic.PlugIns.ItemLevelLimitPlugIn"/> (+11 by default), so it can be raised up to this level in its configuration.
    /// </summary>
    public const byte MaximumItemLevel = 13;

    private const byte OrbsGroup = 12;

    private const byte ScrollsGroup = 15;

    private static readonly IReadOnlySet<byte> AllowedClasses = new HashSet<byte>
    {
        (byte)CharacterClassNumber.DarkWizard,
        (byte)CharacterClassNumber.SoulMaster,
        (byte)CharacterClassNumber.DarkKnight,
        (byte)CharacterClassNumber.BladeKnight,
        (byte)CharacterClassNumber.FairyElf,
        (byte)CharacterClassNumber.MuseElf,
    };

    private static readonly IReadOnlySet<byte> CreatableClasses = new HashSet<byte>
    {
        (byte)CharacterClassNumber.DarkWizard,
        (byte)CharacterClassNumber.DarkKnight,
        (byte)CharacterClassNumber.FairyElf,
    };

    /// <summary>
    /// The items of an unknown version which belong to the classic game and stay active.
    /// All other items of an unknown version are deactivated.
    /// </summary>
    private static readonly IReadOnlySet<(byte Group, short Number)> ClassicItemsOfUnknownVersion = new HashSet<(byte Group, short Number)>
    {
        (13, 20), // Wizard's Ring
        (13, 21), // Ring of Fire
        (13, 22), // Ring of Earth
        (13, 23), // Ring of Wind
        (13, 24), // Ring of Magic
        (13, 25), // Pendant of Ice
        (13, 26), // Pendant of Wind
        (13, 27), // Pendant of Water
        (13, 28), // Pendant of Ability
        (13, 46), // Devil Square Ticket
        (13, 47), // Blood Castle Ticket
        (13, 48), // Kalima Ticket
    };

    /// <summary>
    /// The monsters and NPCs of an unknown version which belong to the classic game and stay active.
    /// All other monsters of an unknown version are deactivated.
    /// </summary>
    private static readonly IReadOnlySet<short> ClassicMonstersOfUnknownVersion = new HashSet<short>
    {
        78, // Golden Goblin
        80, // Golden Lizard King
        81, // Golden Vepar
        82, // Golden Tantallos
        83, // Golden Wheel
        230, // Alex (merchant)
        231, // Thompson the Merchant
        478, // Delgado
    };

    /// <summary>
    /// The maps of <see cref="TargetVersion"/> which the classic server doesn't offer anyway.
    /// </summary>
    private static readonly IReadOnlySet<short> RemovedMaps = new HashSet<short>
    {
        33, // Aida
    };

    /// <summary>
    /// The buff skills which can only be cast on the caster itself, not on other players or party members.
    /// </summary>
    private static readonly IReadOnlySet<short> SelfOnlyBuffSkills = new HashSet<short>
    {
        16, // Soul Barrier (Dark Wizard)
        27, // Greater Defense (Fairy Elf)
        28, // Greater Damage (Fairy Elf)
        48, // Swell Life (Dark Knight)
    };

    /// <summary>
    /// Applies the restriction to the specified game configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(GameConfiguration gameConfiguration)
    {
        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            var isAllowed = AllowedClasses.Contains(characterClass.Number);
            characterClass.IsActive = isAllowed;
            characterClass.CanGetCreated = CreatableClasses.Contains(characterClass.Number);
        }

        foreach (var map in gameConfiguration.Maps.Where(m => IsTooNew(m.IntroducedIn)))
        {
            map.IsActive = false;
        }

        DeactivateRemovedMaps(gameConfiguration);

        foreach (var monster in gameConfiguration.Monsters.Where(m => IsTooNew(m.IntroducedIn)
                     || (m.IntroducedIn == GameVersion.Unknown && !ClassicMonstersOfUnknownVersion.Contains(m.Number))))
        {
            monster.IsActive = false;
        }

        foreach (var miniGame in gameConfiguration.MiniGameDefinitions.Where(m => IsTooNew(m.IntroducedIn)))
        {
            miniGame.IsActive = false;
        }

        foreach (var item in gameConfiguration.Items)
        {
            if (IsTooNew(item.IntroducedIn)
                || (item.IntroducedIn == GameVersion.Unknown && !ClassicItemsOfUnknownVersion.Contains((item.Group, item.Number)))
                || IsOnlyForInactiveClasses(item))
            {
                item.IsActive = false;
            }

            if (item.MaximumItemLevel > MaximumItemLevel)
            {
                item.MaximumItemLevel = MaximumItemLevel;
            }
        }

        RemoveUpgradeCraftingsAboveMaximumLevel(gameConfiguration);
        RemoveSkillLevelRequirements(gameConfiguration);
        RestrictBuffsToSelf(gameConfiguration);
    }

    /// <summary>
    /// Determines whether the game configuration was created by the <see cref="ClassicDataInitialization"/>.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns><c>true</c>, if it's a classic configuration; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// The classic data shares the Season 6 updates, so a classic-only update has to check this.
    /// Only the classic data has the PvP attributes.
    /// </remarks>
    internal static bool IsClassic(GameConfiguration gameConfiguration)
        => gameConfiguration.Attributes.Any(a => a.Id == PvpStats.Week.Id);

    /// <summary>
    /// Removes the character level requirements of the skills and of the items which teach them (scrolls, orbs, parchments).
    /// Other requirements, like energy, stay.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void RemoveSkillLevelRequirements(GameConfiguration gameConfiguration)
    {
        static void RemoveLevelRequirements(ICollection<AttributeRequirement> requirements)
        {
            foreach (var requirement in requirements.Where(r => r.Attribute?.Id == Stats.Level.Id).ToList())
            {
                requirements.Remove(requirement);
            }
        }

        foreach (var skill in gameConfiguration.Skills)
        {
            RemoveLevelRequirements(skill.Requirements);
        }

        foreach (var item in gameConfiguration.Items.Where(IsSkillLearningItem))
        {
            RemoveLevelRequirements(item.Requirements);
        }
    }

    /// <summary>
    /// Restricts the <see cref="SelfOnlyBuffSkills"/> to the caster: a cast on another player is refused,
    /// and Swell Life no longer affects the party members.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void RestrictBuffsToSelf(GameConfiguration gameConfiguration)
    {
        foreach (var skill in gameConfiguration.Skills.Where(s => SelfOnlyBuffSkills.Contains(s.Number)))
        {
            skill.TargetRestriction = SkillTargetRestriction.Self;
            if (skill.Target == SkillTarget.ImplicitParty)
            {
                skill.Target = SkillTarget.ImplicitPlayer;
            }
        }
    }

    /// <summary>
    /// Deactivates the maps which the classic server doesn't offer, although they belong to <see cref="TargetVersion"/>.
    /// Warps to inactive maps are neither listed in the warp window nor usable.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void DeactivateRemovedMaps(GameConfiguration gameConfiguration)
    {
        foreach (var map in gameConfiguration.Maps.Where(m => RemovedMaps.Contains(m.Number)))
        {
            map.IsActive = false;
        }
    }

    private static bool IsTooNew(GameVersion version) => version != GameVersion.Unknown && version > TargetVersion;

    private static bool IsOnlyForInactiveClasses(ItemDefinition item)
        => item.QualifiedCharacters.Count > 0 && item.QualifiedCharacters.All(c => !c.IsActive);

    private static bool IsSkillLearningItem(ItemDefinition item)
        => item.Skill is not null && item.Group is OrbsGroup or ScrollsGroup;

    private static void RemoveUpgradeCraftingsAboveMaximumLevel(GameConfiguration gameConfiguration)
    {
        foreach (var npc in gameConfiguration.Monsters.Where(m => m.ItemCraftings.Count > 0))
        {
            var tooHigh = npc.ItemCraftings
                .Where(crafting => crafting.SimpleCraftingSettings?.RequiredItems
                    .Any(required => required.MinimumItemLevel >= MaximumItemLevel && required.MinimumItemLevel == required.MaximumItemLevel) is true)
                .ToList();
            foreach (var crafting in tooHigh)
            {
                npc.ItemCraftings.Remove(crafting);
            }
        }
    }
}
