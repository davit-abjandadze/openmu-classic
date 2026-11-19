// <copyright file="CharacterAttributeRankingEfCoreTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PvP;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Persistence.EntityFramework;
using MUnique.OpenMU.Persistence.EntityFramework.Json;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Classic;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Tests the ranking query of <see cref="IPlayerContext.GetCharacterAttributeRankingAsync"/> against PostgreSQL,
/// which requires a running postgres database. It recreates the database.
/// </summary>
[TestFixture]
internal class CharacterAttributeRankingEfCoreTests
{
    /// <summary>
    /// Saves characters with PvP values of the current and an old week, and checks the order, the week filter and the values.
    /// </summary>
    [Test]
    [Explicit("It requires a database, which gets recreated.")]
    public async Task RankingIsQueriedByTheDatabaseAsync()
    {
        JsonConverterRegistry.ClearConverters();
        JsonConverterRegistry.RegisterConverter(new LocalizedStringJsonConverter());
        JsonConverterRegistry.RegisterConverter(new BinaryAsHexJsonConverter());

        var contextProvider = new PersistenceContextProvider(new NullLoggerFactory(), null);
        (await contextProvider.ReCreateDatabaseAsync().ConfigureAwait(false)).Dispose();
        await new ClassicDataInitialization(contextProvider, new NullLoggerFactory()).CreateInitialDataAsync(1, false).ConfigureAwait(false);

        using var configurationContext = contextProvider.CreateNewConfigurationContext();
        var configuration = (await configurationContext.GetAsync<GameConfiguration>().ConfigureAwait(false)).Single();
        var characterClass = configuration.CharacterClasses.First(c => c.Number == 4);
        AttributeDefinition Persistent(AttributeDefinition attribute) => configuration.Attributes.First(a => a.Id == attribute.Id);

        using (var playerContext = contextProvider.CreateNewPlayerContext(configuration))
        {
            void Add(string name, float week, float prestige, float kills)
            {
                var account = playerContext.CreateNew<Account>();
                account.LoginName = name.ToLowerInvariant();
                account.PasswordHash = string.Empty;
                account.SecurityCode = string.Empty;
                account.EMail = string.Empty;
                var character = playerContext.CreateNew<Character>();
                character.Name = name;
                character.CharacterClass = characterClass;
                character.CurrentMap = configuration.Maps.First(m => m.Number == 0);
                character.Inventory = playerContext.CreateNew<ItemStorage>();
                character.Attributes.Add(playerContext.CreateNew<StatAttribute>(Persistent(PvpStats.Week), week));
                character.Attributes.Add(playerContext.CreateNew<StatAttribute>(Persistent(PvpStats.WeeklyPrestige), prestige));
                character.Attributes.Add(playerContext.CreateNew<StatAttribute>(Persistent(PvpStats.WeeklyKills), kills));
                account.Characters.Add(character);
            }

            Add("Second", 7, 30, 3);
            Add("First", 7, 50, 5);
            Add("OldWeek", 6, 900, 90);
            Add("Nothing", 7, 0, 0);
            await playerContext.SaveChangesAsync().ConfigureAwait(false);
        }

        using var queryContext = contextProvider.CreateNewPlayerContext(configuration);
        var ids = new[] { PvpStats.WeeklyPrestige.Id, PvpStats.WeeklyKills.Id, PvpStats.WeeklyAssists.Id };
        var week = await queryContext.GetCharacterAttributeRankingAsync(PvpStats.WeeklyPrestige.Id, PvpStats.Week.Id, 7, ids, 10).ConfigureAwait(false);
        var all = await queryContext.GetCharacterAttributeRankingAsync(PvpStats.WeeklyPrestige.Id, null, 0, ids, 2).ConfigureAwait(false);

        Assert.That(week.Select(e => (e.Name, e.Values[0], e.Values[1], e.Values[2])), Is.EqualTo(new[] { ("First", 50f, 5f, 0f), ("Second", 30f, 3f, 0f) }));
        Assert.That(week[0].CharacterClassId, Is.EqualTo(characterClass.GetId()));
        Assert.That(all.Select(e => e.Name), Is.EqualTo(new[] { "OldWeek", "First" }));
    }
}
