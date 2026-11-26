// <copyright file="ClassicCastleSiege.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;

/// <summary>
/// The Castle Siege of the classic server:
/// <list type="bullet">
///   <item>Every guild member who fought at least <see cref="ParticipantRewardMinSeconds"/> in the battle receives
///     <see cref="RewardItemCount"/> Boxes of Kundun +4. Offline participants receive them when they enter the game.</item>
///   <item>Monsters of at least level <see cref="SignOfLordMinimumMonsterLevel"/> drop the Sign of Lord (Rena +3) with a chance of
///     <see cref="SignOfLordDropChance"/>, on all maps which have the common jewel drop. Without it, no guild can register its marks.</item>
///   <item>The suppliers of the Valley of Loren sell potions to the attackers and defenders, see <see cref="SupplierNumbers"/>.</item>
/// </list>
/// </summary>
public static class ClassicCastleSiege
{
    /// <summary>
    /// The number of Boxes of Kundun +4 which each participant receives.
    /// </summary>
    public const int RewardItemCount = 5;

    /// <summary>
    /// The minimum time a participant has to fight in the battle to receive the reward, so that entering the map
    /// shortly before the end isn't enough. The battle takes two hours.
    /// </summary>
    public const int ParticipantRewardMinSeconds = 30 * 60;

    /// <summary>
    /// The drop chance of the Sign of Lord.
    /// </summary>
    public const double SignOfLordDropChance = 0.005;

    /// <summary>
    /// The minimum monster level which drops the Sign of Lord.
    /// </summary>
    public const byte SignOfLordMinimumMonsterLevel = 50;

    /// <summary>
    /// The item level of the Rena which is the Sign of Lord.
    /// </summary>
    public const byte SignOfLordItemLevel = 3;

    private const byte BoxOfLuckGroup = 14;

    private const short BoxOfLuckNumber = 11;

    private const byte RenaGroup = 14;

    private const short RenaNumber = 21;

    private const short JewelsDropGroupNumber = 4;

    /// <summary>
    /// Gets the numbers of the suppliers in the Valley of Loren, Pamela (376) for the attackers and Angela (377) for the defenders,
    /// who sell potions, arrows, bolts and the Town Portal Scroll.
    /// </summary>
    /// <remarks>
    /// They were added in Season 2, so the <see cref="ClassicContentRestriction"/> deactivates them. They sell classic items only,
    /// and without them nobody sells potions on the map during a battle of two hours.
    /// </remarks>
    public static IReadOnlySet<short> SupplierNumbers { get; } = new HashSet<short> { 376, 377 };

    /// <summary>
    /// Applies the Castle Siege changes. It can be applied multiple times with the same result.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void Apply(IContext context, GameConfiguration gameConfiguration)
    {
        ApplyParticipantReward(gameConfiguration);
        ApplySignOfLordDrop(context, gameConfiguration);
    }

    /// <summary>
    /// Activates the <see cref="SupplierNumbers">suppliers</see> in the Valley of Loren.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    public static void ActivateSuppliers(GameConfiguration gameConfiguration)
    {
        foreach (var supplier in gameConfiguration.Monsters.Where(m => SupplierNumbers.Contains(m.Number)))
        {
            supplier.IsActive = true;
        }
    }

    /// <summary>
    /// Gets the identifier of the drop group by which monsters drop the Sign of Lord.
    /// </summary>
    /// <returns>The identifier.</returns>
    internal static Guid GetSignOfLordDropId()
        => GuidHelper.CreateGuid<DropItemGroup>(RenaGroup, RenaNumber, SignOfLordItemLevel);

    private static void ApplyParticipantReward(GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.CastleSiegeConfiguration is not { } configuration
            || gameConfiguration.Items.FirstOrDefault(i => i.Group == BoxOfLuckGroup && i.Number == BoxOfLuckNumber) is not { } boxOfLuck)
        {
            return;
        }

        boxOfLuck.MaximumItemLevel = Math.Max(boxOfLuck.MaximumItemLevel, ClassicBoxes.BoxOfKundunPlus4Level);
        configuration.RewardItemDefinition = boxOfLuck;
        configuration.RewardItemLevel = ClassicBoxes.BoxOfKundunPlus4Level;
        configuration.RewardItemCount = RewardItemCount;
        configuration.ParticipantRewardMinSeconds = ParticipantRewardMinSeconds;
    }

    private static void ApplySignOfLordDrop(IContext context, GameConfiguration gameConfiguration)
    {
        if (gameConfiguration.Items.FirstOrDefault(i => i.Group == RenaGroup && i.Number == RenaNumber) is not { } rena)
        {
            return;
        }

        rena.MaximumItemLevel = Math.Max(rena.MaximumItemLevel, SignOfLordItemLevel);

        var id = GetSignOfLordDropId();
        var dropGroup = gameConfiguration.DropItemGroups.FirstOrDefault(g => g.GetId() == id);
        if (dropGroup is null)
        {
            dropGroup = context.CreateNew<DropItemGroup>();
            dropGroup.SetGuid(id);
            gameConfiguration.DropItemGroups.Add(dropGroup);
        }

        dropGroup.Description = "Sign of Lord (Rena +3, 0.5% drop chance)";
        dropGroup.Chance = SignOfLordDropChance;
        dropGroup.MinimumMonsterLevel = SignOfLordMinimumMonsterLevel;
        dropGroup.MaximumMonsterLevel = null;
        dropGroup.ItemType = SpecialItemType.None;
        dropGroup.ItemLevel = SignOfLordItemLevel;
        if (dropGroup.PossibleItems.Count != 1 || !dropGroup.PossibleItems.Contains(rena))
        {
            dropGroup.PossibleItems.Clear();
            dropGroup.PossibleItems.Add(rena);
        }

        var jewelsId = GuidHelper.CreateGuid<DropItemGroup>(JewelsDropGroupNumber);
        foreach (var map in gameConfiguration.Maps.Where(m => m.DropItemGroups.Any(g => g.GetId() == jewelsId)))
        {
            if (!map.DropItemGroups.Contains(dropGroup))
            {
                map.DropItemGroups.Add(dropGroup);
            }
        }
    }
}
