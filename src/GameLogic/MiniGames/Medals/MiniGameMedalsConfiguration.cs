// <copyright file="MiniGameMedalsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Medals;

/// <summary>
/// The configuration of the <see cref="MiniGameMedalsPlugIn"/>.
/// </summary>
public class MiniGameMedalsConfiguration
{
    /// <summary>
    /// Gets or sets the number of silver medals for the 1st place of the Devil Square.
    /// </summary>
    [Display(Name = "Devil Square silver medals (1st)", Description = "The silver medals for the 1st place of the Devil Square.")]
    public int DevilSquareWinnerSilverMedals { get; set; } = 4;

    /// <summary>
    /// Gets or sets the number of silver medals for the 2nd and 3rd place of the Devil Square.
    /// </summary>
    [Display(Name = "Devil Square silver medals (2nd, 3rd)", Description = "The silver medals for the 2nd and 3rd place of the Devil Square.")]
    public int DevilSquareRunnerUpSilverMedals { get; set; } = 2;

    /// <summary>
    /// Gets or sets the number of silver medals for the winner of the Blood Castle.
    /// </summary>
    [Display(Name = "Blood Castle silver medals (winner)", Description = "The silver medals for the winner of the Blood Castle.")]
    public int BloodCastleWinnerSilverMedals { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of silver medals for each other member of the winning party of the Blood Castle.
    /// </summary>
    [Display(Name = "Blood Castle silver medals (winning party)", Description = "The silver medals for each other member of the party of the Blood Castle winner.")]
    public int BloodCastlePartySilverMedals { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of monster kills in a game, from which a player gets an additional silver medal.
    /// </summary>
    [Display(Name = "Kills for a silver medal", Description = "A player who killed at least this number of monsters in the game gets an additional silver medal. 0 disables it.")]
    public int KillsForSilverMedal { get; set; } = 50;
}
