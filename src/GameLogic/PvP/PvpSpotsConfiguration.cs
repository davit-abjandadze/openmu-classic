// <copyright file="PvpSpotsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PvP;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="PvpSpotsPlugIn"/>.
/// </summary>
public class PvpSpotsConfiguration
{
    /// <summary>
    /// Gets or sets the PvP spots. Killing a player in a spot never makes the killer a player killer and earns prestige.
    /// </summary>
    [Display(Name = "Spots", Description = "The areas in which killing players doesn't make the killer a player killer and earns prestige.")]
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    public ICollection<PvpSpot> Spots { get; set; } = [];

    /// <summary>
    /// Gets or sets the prestige for a kill.
    /// </summary>
    [Display(Name = "Prestige per kill")]
    public int PrestigePerKill { get; set; } = 10;

    /// <summary>
    /// Gets or sets the prestige for a won duel.
    /// </summary>
    [Display(Name = "Prestige per duel victory")]
    public int PrestigePerDuelVictory { get; set; } = 10;

    /// <summary>
    /// Gets or sets the multiplier of the prestige when the victim is stronger than the killer, see <see cref="StrongerOpponentLevelDifference"/>.
    /// </summary>
    [Display(Name = "Stronger opponent multiplier", Description = "The prestige is multiplied by this value, when the victim is stronger than the killer.")]
    public float StrongerOpponentMultiplier { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets the difference of the strength (level + resets * maximum level) above which the victim counts as stronger.
    /// </summary>
    [Display(Name = "Stronger opponent level difference", Description = "The victim counts as stronger, when its level (including 400 levels per reset) is higher than the one of the killer by more than this value.")]
    public int StrongerOpponentLevelDifference { get; set; } = 50;

    /// <summary>
    /// Gets or sets the multiplier of the prestige when the victim is a player killer.
    /// Player killers can be killed for prestige everywhere, not only in spots.
    /// </summary>
    [Display(Name = "Player killer victim multiplier", Description = "The prestige is multiplied by this value, when the victim is a player killer. Player killers give prestige everywhere.")]
    public float PlayerKillerVictimMultiplier { get; set; } = 2;

    /// <summary>
    /// Gets or sets the time in which a hit on the victim counts as an assist for a kill of another player.
    /// </summary>
    [Display(Name = "Assist time", Description = "A hit on the victim within this time before another player killed it counts as an assist.")]
    public TimeSpan AssistTime { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the maximum number of kills of the same victim which give prestige within the <see cref="SameVictimTime"/>.
    /// </summary>
    [Display(Name = "Maximum kills of the same victim", Description = "The maximum number of kills of the same victim which give prestige within the time below. Prevents farming.")]
    public int MaximumKillsOfSameVictim { get; set; } = 3;

    /// <summary>
    /// Gets or sets the time for <see cref="MaximumKillsOfSameVictim"/>.
    /// </summary>
    [Display(Name = "Same victim time")]
    public TimeSpan SameVictimTime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the start of the first week (UTC). Every 7 days from this point in time, a new week starts and the weekly values start over.
    /// </summary>
    [Display(Name = "Week start (UTC)", Description = "A point in time (UTC) at which a week starts, e.g. a Monday 00:00 in the time zone of the server. Every 7 days from it the weekly ranking starts over.")]
    public DateTime WeekStart { get; set; } = new(2023, 12, 31, 20, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Gets or sets the number of entries which are shown by the ranking command.
    /// </summary>
    [Display(Name = "Ranking size")]
    public int RankingSize { get; set; } = 10;

    /// <summary>
    /// Gets the number of the week of the specified point in time.
    /// </summary>
    /// <param name="utcNow">The point in time (UTC).</param>
    /// <returns>The number of the week.</returns>
    public int GetWeekNumber(DateTime utcNow)
    {
        var start = DateTime.SpecifyKind(this.WeekStart, DateTimeKind.Utc);
        return (int)Math.Floor((utcNow - start).TotalDays / 7) + 1;
    }

    /// <summary>
    /// Gets the spot at the specified position.
    /// </summary>
    /// <param name="mapNumber">The map number.</param>
    /// <param name="x">The x coordinate.</param>
    /// <param name="y">The y coordinate.</param>
    /// <returns>The spot, or <c>null</c> if the position is not in a spot.</returns>
    public PvpSpot? GetSpot(int mapNumber, byte x, byte y)
    {
        return this.Spots.FirstOrDefault(spot => spot.Contains(mapNumber, x, y));
    }

    /// <summary>
    /// An area in which players can kill each other without becoming a player killer.
    /// </summary>
    public class PvpSpot
    {
        /// <summary>
        /// Gets or sets the name of the spot.
        /// </summary>
        [Display(Name = "Name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of the map.
        /// </summary>
        [Display(Name = "Map number")]
        public short MapNumber { get; set; }

        /// <summary>
        /// Gets or sets the minimum x coordinate.
        /// </summary>
        [Display(Name = "X1")]
        public byte X1 { get; set; }

        /// <summary>
        /// Gets or sets the minimum y coordinate.
        /// </summary>
        [Display(Name = "Y1")]
        public byte Y1 { get; set; }

        /// <summary>
        /// Gets or sets the maximum x coordinate.
        /// </summary>
        [Display(Name = "X2")]
        public byte X2 { get; set; }

        /// <summary>
        /// Gets or sets the maximum y coordinate.
        /// </summary>
        [Display(Name = "Y2")]
        public byte Y2 { get; set; }

        /// <summary>
        /// Determines whether the spot contains the specified position.
        /// </summary>
        /// <param name="mapNumber">The map number.</param>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns><c>true</c>, if the spot contains the position.</returns>
        public bool Contains(int mapNumber, byte x, byte y)
            => mapNumber == this.MapNumber && x >= this.X1 && x <= this.X2 && y >= this.Y1 && y <= this.Y2;

        /// <inheritdoc />
        public override string ToString() => $"{this.Name} (map {this.MapNumber}: {this.X1},{this.Y1} - {this.X2},{this.Y2})";
    }
}
