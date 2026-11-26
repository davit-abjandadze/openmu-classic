// <copyright file="ClassicInstancedEventsUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.MiniGames;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Replaces the nightly Devil Square and Blood Castle of an existing classic (Season 1 content) database by
/// three slots a day, in which every solo player or party enters its own instance of the tier of its level,
/// once per day and event (see <see cref="ClassicEvents"/>).
/// </summary>
/// <remarks>
/// The classic data shares the Season 6 updates, so this update does nothing on a regular Season 6 database.
/// </remarks>
[PlugIn]
[Display(Name = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicInstancedEventsUpdatePlugIn_Name), Description = nameof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources.ClassicInstancedEventsUpdatePlugIn_Description), ResourceType = typeof(MUnique.OpenMU.Persistence.Initialization.Properties.PlugInResources))]
[Guid("71936918-1541-4A21-90A5-EE63AF83B0CE")]
public class ClassicInstancedEventsUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Classic: Devil Square and Blood Castle three times a day in instances";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Replaces the nightly Devil Square (21:00) and Blood Castle (22:00) by three slots a day (Devil Square 13:00, 19:00, 22:00; Blood Castle 13:30, 19:30, 22:30) in which every solo player or party enters its own instance of the tier of its level (6 Devil Square and 7 Blood Castle tiers), once per day and event. Overwrites the configuration of both start plugins and their mini game definitions. Only applies to the classic (Season 1 content) data.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => true;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 11, 26, 0, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        if (!ClassicContentRestriction.IsClassic(gameConfiguration))
        {
            return ValueTask.CompletedTask;
        }

        ClassicEvents.ApplyToMiniGames(gameConfiguration);
        ClassicEvents.ApplyToEventStartPlugIns(gameConfiguration, new ConfigureNpcTalkPlugInsPlugInBase.IdReferenceHandler());
        AddEntryDayAttributes(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Adds the attributes of the daily entries to every character class, so they're persisted with the characters.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    private static void AddEntryDayAttributes(IContext context, GameConfiguration gameConfiguration)
    {
        foreach (var attribute in MiniGameEntryStats.All)
        {
            var persistent = gameConfiguration.Attributes.FirstOrDefault(a => a.Id == attribute.Id);
            if (persistent is null)
            {
                persistent = context.CreateNew<AttributeDefinition>(attribute.Id, attribute.Designation, attribute.Description);
                gameConfiguration.Attributes.Add(persistent);
            }

            foreach (var characterClass in gameConfiguration.CharacterClasses.Where(c => c.StatAttributes.All(a => a.Attribute != persistent)))
            {
                characterClass.StatAttributes.Add(context.CreateNew<StatAttributeDefinition>(persistent, 0, false));
            }
        }
    }
}
