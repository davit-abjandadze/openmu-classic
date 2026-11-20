// <copyright file="MedalMerchantConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="MedalMerchantPlugIn"/>.
/// </summary>
public class MedalMerchantConfiguration
{
    /// <summary>
    /// The number of Thompson the Merchant, who is the medal merchant of the classic server.
    /// </summary>
    public const short DefaultMerchantNumber = 231;

    /// <summary>
    /// The level of the Box of Luck which is the Box of Kundun +1, as the client defines it. The +5 is four levels above.
    /// </summary>
    private const byte FirstBoxOfKundunLevel = 8;

    /// <summary>
    /// Gets or sets the number of the merchant NPC which sells its items for silver medals instead of zen.
    /// </summary>
    [Display(Name = "Merchant number", Description = "The number of the merchant NPC (monster definition) which sells its items for silver medals instead of zen.")]
    public short MerchantNumber { get; set; } = DefaultMerchantNumber;

    /// <summary>
    /// Gets or sets the items of the merchant with their prices. The merchant offers exactly these items, in this order,
    /// instead of the items of its merchant store, so items can be added and removed while the server is running.
    /// Items which don't fit into the store or whose options their definition doesn't allow are left out.
    /// </summary>
    [Display(Name = "Items", Description = "The items which the merchant sells, with their silver medal prices. Changes are applied while the server is running, the next time a player opens the store.")]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<MedalPrice> Prices { get; set; } = [];

    /// <summary>
    /// Creates the default configuration, in which Thompson the Merchant sells the Box of Kundun +4 for 10
    /// and the Box of Kundun +5 for 15 silver medals.
    /// </summary>
    /// <remarks>
    /// A boss drops its box and one silver medal (the Dark Phoenix three), so the medals of 10 Zaikan or Death Beam Knight kills
    /// buy one more Box of Kundun +4, and of 5 Dark Phoenix kills one more +5.
    /// The lower boxes aren't sold, they come from the bosses, the hunting points and the gold medals.
    /// </remarks>
    /// <returns>The default configuration.</returns>
    public static MedalMerchantConfiguration CreateDefault()
    {
        var configuration = new MedalMerchantConfiguration();
        configuration.Prices.Add(CreateBoxOfKundunPrice(4, 10));
        configuration.Prices.Add(CreateBoxOfKundunPrice(5, 15));
        return configuration;
    }

    /// <summary>
    /// Gets the price of the item, if it has one.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The price, or <c>null</c>.</returns>
    public MedalPrice? GetPrice(Item item)
        => item.Definition is { } definition
            ? this.Prices.FirstOrDefault(p => p.ItemGroup == definition.Group && p.ItemNumber == definition.Number && p.ItemLevel == item.Level)
            : null;

    private static MedalPrice CreateBoxOfKundunPrice(byte plus, int silverMedals) => new()
    {
        Name = $"Box of Kundun +{plus}",
        ItemGroup = MedalItems.Group,
        ItemNumber = MedalItems.Number,
        ItemLevel = (byte)(FirstBoxOfKundunLevel + plus - 1),
        SilverMedals = silverMedals,
    };

    /// <summary>
    /// The silver medal price of an item.
    /// </summary>
    public class MedalPrice
    {
        /// <summary>
        /// Gets or sets the name which is shown. When it's empty, the name of the item with its level is shown.
        /// </summary>
        [Display(Name = "Name", Description = "The shown name. When it's empty, the name of the item with its level is shown.")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the group of the item.
        /// </summary>
        [Display(Name = "Item group")]
        public byte ItemGroup { get; set; }

        /// <summary>
        /// Gets or sets the number of the item.
        /// </summary>
        [Display(Name = "Item number")]
        public short ItemNumber { get; set; }

        /// <summary>
        /// Gets or sets the level of the item.
        /// </summary>
        [Display(Name = "Item level")]
        public byte ItemLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item has its skill.
        /// </summary>
        [Display(Name = "Skill")]
        public bool Skill { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the item has luck.
        /// </summary>
        [Display(Name = "Luck")]
        public bool Luck { get; set; }

        /// <summary>
        /// Gets or sets the level of the additional option of the item, or the bit mask of its fixed options (e.g. of the Dinorant).
        /// </summary>
        [Display(Name = "Option level", Description = "The level of the additional option (e.g. 1 to 4 for +4 to +16), or the bit mask of the fixed options (e.g. of the Dinorant).")]
        public byte OptionLevel { get; set; }

        /// <summary>
        /// Gets or sets the bit mask of the excellent options of the item.
        /// </summary>
        [Display(Name = "Excellent options", Description = "The bit mask of the excellent options, e.g. 63 for all six.")]
        public byte ExcellentOptions { get; set; }

        /// <summary>
        /// Gets or sets the price in silver medals.
        /// </summary>
        [Display(Name = "Silver medals")]
        public int SilverMedals { get; set; }
    }
}
