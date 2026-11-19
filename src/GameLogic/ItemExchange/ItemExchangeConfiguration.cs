// <copyright file="ItemExchangeConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.ItemExchange;

/// <summary>
/// The configuration of the <see cref="ItemExchangeChatCommandPlugIn"/>.
/// </summary>
public class ItemExchangeConfiguration
{
    /// <summary>
    /// Gets or sets the number of excellent items which are exchanged for one target item.
    /// </summary>
    [Display(Name = "Required excellent items")]
    public int RequiredItems { get; set; } = 3;

    /// <summary>
    /// Gets or sets the minimum drop level of the excellent items which are exchanged.
    /// </summary>
    [Display(Name = "Minimum drop level")]
    public int MinimumDropLevel { get; set; } = 44;

    /// <summary>
    /// Gets or sets the chance in percent of a second excellent option.
    /// </summary>
    [Display(Name = "Chance of two excellent options (%)")]
    public int TwoOptionsChance { get; set; } = 25;

    /// <summary>
    /// Gets or sets the chance in percent of a third excellent option.
    /// </summary>
    [Display(Name = "Chance of three excellent options (%)")]
    public int ThreeOptionsChance { get; set; } = 5;

    /// <summary>
    /// Gets or sets the chance in percent of the luck option.
    /// </summary>
    [Display(Name = "Chance of luck (%)")]
    public int LuckChance { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum level of the additional option (1 = +4, 2 = +8, 3 = +12). It's random from none up to this level.
    /// </summary>
    [Display(Name = "Maximum option level", Description = "The additional option is random from none up to this level (1 = +4, 2 = +8, 3 = +12).")]
    public int MaximumOptionLevel { get; set; } = 3;

    /// <summary>
    /// Gets or sets the names of the items which can be obtained.
    /// </summary>
    [Display(Name = "Target items", Description = "The names of the items which can be obtained, e.g. 'Dragon Armor'.")]
    public IList<string> TargetItems { get; set; } =
    [
        "Dragon Helm", "Dragon Armor", "Dragon Pants", "Dragon Gloves", "Dragon Boots",
        "Legendary Helm", "Legendary Armor", "Legendary Pants", "Legendary Gloves", "Legendary Boots",
        "Guardian Helm", "Guardian Armor", "Guardian Pants", "Guardian Gloves", "Guardian Boots",
        "Legendary Sword", "Double Blade", "Giant Sword", "Heliacal Sword", "Lighting Sword",
        "Larkan Axe", "Crescent Axe",
        "Serpent Spear", "Great Scythe",
        "Serpent Crossbow", "Silver Bow",
        "Gorgon Staff", "Legendary Staff",
        "Serpent Shield", "Legendary Shield", "Bronze Shield", "Dragon Shield",
    ];
}
