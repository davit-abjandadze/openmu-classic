// <copyright file="MiniGameLevelSync.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Weakens a character whose level is above the maximum level of the entered mini game tier,
/// so that it fights about like a character of that maximum level (level sync).
/// </summary>
/// <remarks>
/// A real level sync would need the stats of the character at the lower level, which don't exist:
/// in MU, damage, defense and rates come from the distributed stat points and the items, not from
/// the level. Because the stat points grow about linearly with the level, the combat values are
/// multiplied by <c>maximum level / character level</c> instead. The items, skills and the shown
/// level stay as they are. The factor is only added to the attributes in memory and never persisted,
/// so nothing remains on the character after a crash or a disconnect: the attributes are built from
/// the stored values again when the character enters the world.
/// </remarks>
public sealed class MiniGameLevelSync
{
    /// <summary>
    /// The attributes which are weakened by the level sync.
    /// </summary>
    internal static readonly IReadOnlyList<AttributeDefinition> SyncedAttributes =
    [
        Stats.MinimumPhysBaseDmg,
        Stats.MaximumPhysBaseDmg,
        Stats.MinimumWizBaseDmg,
        Stats.MaximumWizBaseDmg,
        Stats.DefenseBase,
        Stats.AttackRatePvm,
        Stats.DefenseRatePvm,
        Stats.MaximumHealth,
        Stats.MaximumMana,
    ];

    private readonly IAttributeSystem _attributes;

    private readonly List<(IElement Element, AttributeDefinition Target)> _elements = new();

    private MiniGameLevelSync(IAttributeSystem attributes, float factor)
    {
        this._attributes = attributes;
        this.Factor = factor;
    }

    /// <summary>
    /// Gets the factor by which the combat values are multiplied.
    /// </summary>
    public float Factor { get; }

    /// <summary>
    /// Calculates the factor of the level sync.
    /// </summary>
    /// <param name="level">The level of the character.</param>
    /// <param name="maximumLevel">The maximum level of the tier.</param>
    /// <returns>The factor; 1, if the character doesn't need to be weakened.</returns>
    public static float CalculateFactor(int level, int maximumLevel)
    {
        if (maximumLevel <= 0 || level <= maximumLevel)
        {
            return 1;
        }

        return (float)maximumLevel / level;
    }

    /// <summary>
    /// Weakens the attributes by the factor.
    /// </summary>
    /// <param name="attributes">The attributes of the character.</param>
    /// <param name="factor">The factor, see <see cref="CalculateFactor"/>.</param>
    /// <returns>The applied level sync, which has to be removed when the character leaves the mini game; or <c>null</c>, if the factor doesn't weaken.</returns>
    public static MiniGameLevelSync? Apply(IAttributeSystem attributes, float factor)
    {
        if (factor >= 1 || factor <= 0)
        {
            return null;
        }

        var sync = new MiniGameLevelSync(attributes, factor);
        foreach (var attribute in SyncedAttributes)
        {
            // An attribute without a value would get the factor as its value, because a composable
            // attribute with only multiplicative elements uses 1 as its raw value.
            if (attributes[attribute] <= 0)
            {
                continue;
            }

            var element = new ConstantElement(factor, AggregateType.Multiplicate);
            attributes.AddElement(element, attribute);
            sync._elements.Add((element, attribute));
        }

        // The current values must not stay above the lowered maximums.
        LimitCurrentValue(attributes, Stats.CurrentHealth, Stats.MaximumHealth);
        LimitCurrentValue(attributes, Stats.CurrentMana, Stats.MaximumMana);
        return sync;
    }

    /// <summary>
    /// Removes the level sync, so that the attributes are restored.
    /// </summary>
    public void Remove()
    {
        foreach (var (element, target) in this._elements)
        {
            this._attributes.RemoveElement(element, target);
        }

        this._elements.Clear();
    }

    private static void LimitCurrentValue(IAttributeSystem attributes, AttributeDefinition current, AttributeDefinition maximum)
    {
        var maximumValue = attributes[maximum];
        if (attributes[current] > maximumValue)
        {
            attributes[current] = maximumValue;
        }
    }
}
