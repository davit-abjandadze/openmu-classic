// <copyright file="StartingInventoryExtensionsPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.Persistence;
using MUnique.OpenMU.Persistence.InMemory;

/// <summary>
/// Tests for the <see cref="StartingInventoryExtensionsPlugIn"/>.
/// </summary>
[TestFixture]
public class StartingInventoryExtensionsPlugInTest
{
    private IContext _context = null!;

    /// <summary>
    /// Sets up the persistence context.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        this._context = new InMemoryPersistenceContextProvider().CreateNewContext();
    }

    /// <summary>
    /// Disposes the persistence context.
    /// </summary>
    [TearDown]
    public void TearDown()
    {
        this._context.Dispose();
    }

    /// <summary>
    /// Tests that a created character gets all inventory extensions with the default configuration.
    /// </summary>
    [Test]
    public void CreatedCharacterGetsAllExtensions()
    {
        var character = this._context.CreateNew<Character>();

        new StartingInventoryExtensionsPlugIn().CharacterCreated(this._context.CreateNew<Account>(), character, this._context, this._context.CreateNew<GameConfiguration>(), NullLogger.Instance);

        Assert.That(character.InventoryExtensions, Is.EqualTo(InventoryConstants.MaximumNumberOfExtensions));
    }

    /// <summary>
    /// Tests that an existing character with fewer extensions gets the configured number of extensions,
    /// which can't exceed the number the client supports.
    /// </summary>
    [Test]
    public void ConfiguredNumberIsLimitedToTheClientMaximum()
    {
        var character = this._context.CreateNew<Character>();
        character.InventoryExtensions = 1;
        var plugIn = new StartingInventoryExtensionsPlugIn { Configuration = new StartingInventoryExtensionsConfiguration { Extensions = 10 } };

        plugIn.CharacterEnteringWorld(null!, character);

        Assert.That(character.InventoryExtensions, Is.EqualTo(InventoryConstants.MaximumNumberOfExtensions));
    }

    /// <summary>
    /// Tests that a character never loses extensions it already has.
    /// </summary>
    [Test]
    public void ExtensionsAreNeverTakenAway()
    {
        var character = this._context.CreateNew<Character>();
        character.InventoryExtensions = 3;
        var plugIn = new StartingInventoryExtensionsPlugIn { Configuration = new StartingInventoryExtensionsConfiguration { Extensions = 2 } };

        plugIn.CharacterEnteringWorld(null!, character);

        Assert.That(character.InventoryExtensions, Is.EqualTo(3));
    }
}
