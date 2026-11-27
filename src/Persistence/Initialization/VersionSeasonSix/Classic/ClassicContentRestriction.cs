// <copyright file="ClassicContentRestriction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.Persistence.Initialization.CharacterClasses;
using MUnique.OpenMU.Persistence.Initialization.Skills;

/// <summary>
/// Restricts the content of the season 6 data to a classic server:
/// <list type="bullet">
///   <item>Only Dark Wizard, Dark Knight and Fairy Elf (and their second classes) are active and can be created.</item>
///   <item>Maps, monsters, items and mini games which were introduced after <see cref="TargetVersion"/> are deactivated.</item>
///   <item>Items can't be upgraded above <see cref="MaximumItemLevel"/>.</item>
///   <item>Skills, and the scrolls and orbs which teach them, have no character level requirement.</item>
///   <item>The scrolls of Decay, Ice Storm and Nova are deactivated, see <see cref="RemovedSkillItems"/>.</item>
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

    // Energy shown for each playable level of the Orb of Summoning in the classic client.
    private static readonly int[] SummonEnergyByOrbLevel = [30, 60, 90, 130, 170, 210, 300];

    private static readonly IReadOnlySet<byte> AllowedClasses = new HashSet<byte>
    {
        (byte)CharacterClassNumber.DarkWizard,
        (byte)CharacterClassNumber.SoulMaster,
        (byte)CharacterClassNumber.DarkKnight,
        (byte)CharacterClassNumber.BladeKnight,
        (byte)CharacterClassNumber.FairyElf,
        (byte)CharacterClassNumber.MuseElf,
        (byte)CharacterClassNumber.MagicGladiator,
    };

    private static readonly IReadOnlySet<byte> CreatableClasses = new HashSet<byte>
    {
        (byte)CharacterClassNumber.DarkWizard,
        (byte)CharacterClassNumber.DarkKnight,
        (byte)CharacterClassNumber.FairyElf,
        (byte)CharacterClassNumber.MagicGladiator,
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
    /// The skill scrolls which the classic server doesn't offer. They neither drop nor are sold.
    /// Characters which already learned the skill keep it.
    /// </summary>
    private static readonly IReadOnlySet<(byte Group, short Number)> RemovedSkillItems = new HashSet<(byte Group, short Number)>
    {
        (ScrollsGroup, 16), // Scroll of Decay
        (ScrollsGroup, 17), // Scroll of Ice Storm
        (ScrollsGroup, 18), // Scroll of Nova
    };

    /// <summary>
    /// Applies the restriction to the specified game configuration.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(GameConfiguration gameConfiguration)
    {
        ApplyClassRestrictions(gameConfiguration);
        RemoveAtlansAttackSkills(gameConfiguration);

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
            if (IsExcluded(item))
            {
                item.IsActive = false;
            }

            if (item.MaximumItemLevel > MaximumItemLevel)
            {
                item.MaximumItemLevel = MaximumItemLevel;
            }
        }

        DeactivateRemovedSkillItems(gameConfiguration);
        RemoveUpgradeCraftingsAboveMaximumLevel(gameConfiguration);
        RemoveSkillLevelRequirements(gameConfiguration);
    }

    /// <summary>
    /// Makes Lizard King and Silver Valkyrie use their normal attacks.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void RemoveAtlansAttackSkills(GameConfiguration gameConfiguration)
    {
        foreach (var monster in gameConfiguration.Monsters.Where(m => m.Number is 48 or 52))
        {
            monster.AttackSkill = null;
        }
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
    /// Aligns the seven elf summoning skills with the energy displayed on their orbs.
    /// </summary>
    /// <param name="context">The persistence context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ApplySummonEnergy(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var skill in gameConfiguration.Skills.Where(s => s.Number >= (short)SkillNumber.SummonGoblin && s.Number <= (short)SkillNumber.SummonSoldier))
        {
            var requirement = skill.Requirements.FirstOrDefault(r => r.Attribute?.Id == Stats.TotalEnergy.Id);
            if (requirement is null)
            {
                requirement = context.CreateNew<AttributeRequirement>();
                requirement.Attribute = Stats.TotalEnergy.GetPersistent(gameConfiguration);
                skill.Requirements.Add(requirement);
            }

            requirement.MinimumValue = SummonEnergyByOrbLevel[skill.Number - (short)SkillNumber.SummonGoblin];
        }
    }

    /// <summary>
    /// Restores the original targets of the buffs of Dark Wizard, Dark Knight and Fairy Elf, which an earlier
    /// version of the classic data restricted to the caster: Soul Barrier can be cast on party members,
    /// Greater Defense and Greater Damage on any player, and Swell Life affects the whole party.
    /// The values are the same as in the Season 6 skill initialization.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void RestoreBuffTargets(GameConfiguration gameConfiguration)
    {
        foreach (var skill in gameConfiguration.Skills)
        {
            switch ((SkillNumber)skill.Number)
            {
                case SkillNumber.SoulBarrier:
                    skill.TargetRestriction = SkillTargetRestriction.Party;
                    break;
                case SkillNumber.GreaterDefense:
                case SkillNumber.GreaterDamage:
                    skill.TargetRestriction = SkillTargetRestriction.Player;
                    break;
                case SkillNumber.SwellLife:
                    skill.TargetRestriction = SkillTargetRestriction.Undefined;
                    skill.Target = SkillTarget.ImplicitParty;
                    break;
                default:
                    // other skills are not affected.
                    break;
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

    /// <summary>
    /// Deactivates the <see cref="RemovedSkillItems"/> (Scroll of Decay, Ice Storm and Nova), so they don't drop.
    /// The <see cref="ClassicMerchantStores"/> remove inactive scrolls and orbs from the stores.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void DeactivateRemovedSkillItems(GameConfiguration gameConfiguration)
    {
        foreach (var item in gameConfiguration.Items.Where(i => RemovedSkillItems.Contains((i.Group, i.Number))))
        {
            item.IsActive = false;
        }
    }

    /// <summary>
    /// Activates the classes which the classic server offers (e.g. the Magic Gladiator, which was added later)
    /// and the items which were only deactivated because their classes were inactive, in an existing database.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    internal static void ActivateAllowedClasses(GameConfiguration gameConfiguration)
    {
        ApplyClassRestrictions(gameConfiguration);
        foreach (var item in gameConfiguration.Items.Where(i => !i.IsActive
                     && i.QualifiedCharacters.Any(c => c.IsActive)
                     && !IsExcluded(i)
                     && !RemovedSkillItems.Contains((i.Group, i.Number))))
        {
            item.IsActive = true;
        }
    }

    private static void ApplyClassRestrictions(GameConfiguration gameConfiguration)
    {
        foreach (var characterClass in gameConfiguration.CharacterClasses)
        {
            characterClass.IsActive = AllowedClasses.Contains(characterClass.Number);
            characterClass.CanGetCreated = CreatableClasses.Contains(characterClass.Number);
        }
    }

    private static bool IsExcluded(ItemDefinition item)
        => IsTooNew(item.IntroducedIn)
           || (item.IntroducedIn == GameVersion.Unknown && !ClassicItemsOfUnknownVersion.Contains((item.Group, item.Number)))
           || IsOnlyForInactiveClasses(item);

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
