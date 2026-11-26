// <copyright file="ClassicDataInitialization.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.HuntingRewards;
using MUnique.OpenMU.GameLogic.MiniGames.Medals;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.GameLogic.TeamVsTeam;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Provides initial data for a classic server which runs with the Season 6 Episode 3 client,
/// but only offers the content up to Season 1 with the classes Dark Wizard, Dark Knight and Fairy Elf.
/// </summary>
[Guid("5B0C8E43-2C0E-4F7A-9D55-3E1C7A6B1A01")]
[PlugIn]
[Display(Name = "Season 6 client - Classic Season 1 (DK, DW, Elf)", Description = "Season 6 Episode 3 data, restricted to the content up to Season 1 and to the classes Dark Wizard, Dark Knight and Fairy Elf.")]
public class ClassicDataInitialization : DataInitialization
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassicDataInitialization" /> class.
    /// </summary>
    /// <param name="persistenceContextProvider">The persistence context provider.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    public ClassicDataInitialization(IPersistenceContextProvider persistenceContextProvider, ILoggerFactory loggerFactory)
        : base(persistenceContextProvider, loggerFactory)
    {
    }

    /// <summary>
    /// Gets the identifier, by which the initialization is selected.
    /// </summary>
    public static new string Id => "season6classic";

    /// <inheritdoc />
    public override string Key => Id;

    /// <inheritdoc />
    public override string Caption => "Season 6 client - Classic Season 1 (DK, DW, Elf)";

    /// <inheritdoc />
    protected override string? DefaultTimeZoneId => "Asia/Tbilisi";

    /// <inheritdoc />
    /// <remarks>
    /// The classic data is the Season 6 data with restricted content, so the Season 6 updates apply to it.
    /// </remarks>
    protected override string UpdateKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    protected override async ValueTask OnGameConfigurationInitializedAsync()
    {
        await base.OnGameConfigurationInitializedAsync().ConfigureAwait(false);
        ClassicContentRestriction.Apply(this.GameConfiguration);
        await ClassicRates.ApplyAsync(this.Context, this.GameConfiguration).ConfigureAwait(false);
        ClassicEvents.ApplyToMiniGames(this.GameConfiguration);
        await ClassicMerchantStores.ApplyAsync(this.Context, this.GameConfiguration).ConfigureAwait(false);
        await ClassicBoxes.ApplyAsync(this.Context, this.GameConfiguration).ConfigureAwait(false);
        ClassicBosses.Apply(this.Context, this.GameConfiguration);
        ClassicIcarusMonsters.Apply(this.Context, this.GameConfiguration);
        ClassicFarmSpots.Apply(this.Context, this.GameConfiguration);
        await ClassicMedalMerchant.ApplyAsync(this.Context, this.GameConfiguration).ConfigureAwait(false);
        this.AddCharacterStatAttributes(PvpStats.All.Concat(HuntingJewelStats.All).Concat(MedalStats.All).Concat(HuntingPointStats.All).Concat(TeamVsTeamStats.All));
    }

    /// <inheritdoc />
    protected override void OnPlugInsConfigured(ReferenceHandler referenceHandler)
    {
        base.OnPlugInsConfigured(referenceHandler);
        ClassicPlugInDefaults.Apply(this.GameConfiguration, referenceHandler);
        ClassicEvents.ApplyToPlugIns(this.GameConfiguration, referenceHandler);
    }

    /// <summary>
    /// Adds the attributes as stat attributes to all character classes, so that they are persisted with the characters.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    private void AddCharacterStatAttributes(IEnumerable<AttributeDefinition> attributes)
    {
        foreach (var attribute in attributes)
        {
            var persistent = this.Context.CreateNew<AttributeDefinition>(attribute.Id, attribute.Designation, attribute.Description);
            this.GameConfiguration.Attributes.Add(persistent);
            foreach (var characterClass in this.GameConfiguration.CharacterClasses)
            {
                characterClass.StatAttributes.Add(this.Context.CreateNew<StatAttributeDefinition>(persistent, 0, false));
            }
        }
    }
}
