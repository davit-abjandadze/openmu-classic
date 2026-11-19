// <copyright file="MiniGameMedalsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="MiniGameMedalsPlugIn"/>.
/// </summary>
public class MiniGameMedalsConfiguration
{
    /// <summary>
    /// Gets or sets the number of silver medals for the 2nd and 3rd place of the Devil Square.
    /// </summary>
    [Display(Name = "Devil Square silver medals (2nd, 3rd)", Description = "The silver medals for the 2nd and 3rd place of the Devil Square. The 1st place gets a gold medal.")]
    public int DevilSquareRunnerUpSilverMedals { get; set; } = 2;

    /// <summary>
    /// Gets or sets the number of silver medals for each other member of the winning party of the Blood Castle.
    /// </summary>
    [Display(Name = "Blood Castle silver medals (winning party)", Description = "The silver medals for each other member of the party of the Blood Castle winner, who gets a gold medal.")]
    public int BloodCastlePartySilverMedals { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of monster kills in a game, from which a player gets an additional silver medal.
    /// </summary>
    [Display(Name = "Kills for a silver medal", Description = "A player who killed at least this number of monsters in the game gets an additional silver medal. 0 disables it.")]
    public int KillsForSilverMedal { get; set; } = 50;

    /// <summary>
    /// Gets or sets the offers, for which medals can be exchanged with the /medals command.
    /// </summary>
    [Display(Name = "Exchange offers")]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<MedalOffer> Offers { get; set; } = [];

    /// <summary>
    /// Creates the default configuration with offers for jewels and a Box of Kundun.
    /// </summary>
    /// <returns>The default configuration.</returns>
    public static MiniGameMedalsConfiguration CreateDefault()
    {
        var configuration = new MiniGameMedalsConfiguration();
        configuration.Offers.Add(new MedalOffer { Key = "chaos", SilverMedals = 1, ItemGroup = 12, ItemNumber = 15 });
        configuration.Offers.Add(new MedalOffer { Key = "bless", SilverMedals = 2, ItemGroup = 14, ItemNumber = 13 });
        configuration.Offers.Add(new MedalOffer { Key = "soul", SilverMedals = 2, ItemGroup = 14, ItemNumber = 14 });
        configuration.Offers.Add(new MedalOffer { Key = "life", SilverMedals = 3, ItemGroup = 14, ItemNumber = 16 });
        configuration.Offers.Add(new MedalOffer { Key = "creation", SilverMedals = 3, ItemGroup = 14, ItemNumber = 22 });
        configuration.Offers.Add(new MedalOffer { Key = "box", GoldMedals = 1, ItemGroup = 14, ItemNumber = 11, ItemLevel = 10 });
        return configuration;
    }

    /// <summary>
    /// An offer, for which medals can be exchanged.
    /// </summary>
    public class MedalOffer
    {
        /// <summary>
        /// Gets or sets the key, with which the offer is selected, e.g. "/medals bless".
        /// </summary>
        [Display(Name = "Key")]
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the required gold medals.
        /// </summary>
        [Display(Name = "Gold medals")]
        public int GoldMedals { get; set; }

        /// <summary>
        /// Gets or sets the required silver medals.
        /// </summary>
        [Display(Name = "Silver medals")]
        public int SilverMedals { get; set; }

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
    }
}
