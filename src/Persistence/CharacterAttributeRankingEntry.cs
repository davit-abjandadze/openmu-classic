// <copyright file="CharacterAttributeRankingEntry.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence;

/// <summary>
/// An entry of a ranking of characters by an attribute value.
/// </summary>
/// <param name="Name">The name of the character.</param>
/// <param name="CharacterClassId">The identifier of the character class.</param>
/// <param name="Values">The values of the requested attributes, in the order in which they were requested. Missing attributes are 0.</param>
public sealed record CharacterAttributeRankingEntry(string Name, Guid? CharacterClassId, IReadOnlyList<float> Values);
