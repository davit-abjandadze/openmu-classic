// <copyright file="ClassicResetRulesTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.Resets;

/// <summary>
/// Tests for the rising required level and server reset limit of the <see cref="ResetConfiguration"/>,
/// and for the <see cref="ResetExperienceRateConfiguration"/>.
/// </summary>
[TestFixture]
public class ClassicResetRulesTest
{
    private static readonly DateTime Start = new(2026, 11, 19, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Tests that the required level rises by 10 per reset from 350 up to 400.
    /// </summary>
    [TestCase(0, 350)]
    [TestCase(1, 360)]
    [TestCase(2, 370)]
    [TestCase(3, 380)]
    [TestCase(4, 390)]
    [TestCase(5, 400)]
    [TestCase(30, 400)]
    public void RequiredLevelRisesUpToMaximum(int currentResets, int expectedLevel)
    {
        var configuration = new ResetConfiguration { RequiredLevel = 350, RequiredLevelIncreasePerReset = 10, MaximumRequiredLevel = 400 };
        Assert.That(configuration.GetRequiredLevel(currentResets), Is.EqualTo(expectedLevel));
    }

    /// <summary>
    /// Tests that the required level stays constant without an increase.
    /// </summary>
    [Test]
    public void RequiredLevelIsConstantByDefault()
    {
        var configuration = new ResetConfiguration { RequiredLevel = 400 };
        Assert.That(configuration.GetRequiredLevel(20), Is.EqualTo(400));
    }

    /// <summary>
    /// Tests that the server reset limit rises by one per day, up to the reset limit.
    /// </summary>
    [TestCase(-5.0, 1)]
    [TestCase(0.0, 1)]
    [TestCase(23.9, 1)]
    [TestCase(24.0, 2)]
    [TestCase(24 * 10.5, 11)]
    [TestCase(24 * 54.0, 55)]
    [TestCase(24 * 100.0, 55)]
    public void ServerResetLimitRisesDaily(double hoursAfterStart, int expectedLimit)
    {
        var configuration = CreateServerLimitConfiguration();
        Assert.That(configuration.GetEffectiveResetLimit(Start.AddHours(hoursAfterStart)), Is.EqualTo(expectedLimit));
    }

    /// <summary>
    /// Tests the point in time of the next rise of the server reset limit, and that it doesn't rise after reaching the limit.
    /// </summary>
    [Test]
    public void NextServerResetLimitRise()
    {
        var configuration = CreateServerLimitConfiguration();
        Assert.That(configuration.GetNextServerResetLimitRise(Start.AddHours(30)), Is.EqualTo(Start.AddDays(2)));
        Assert.That(configuration.GetNextServerResetLimitRise(Start.AddDays(60)), Is.Null);
    }

    /// <summary>
    /// Tests that only the reset limit applies without a server reset limit start.
    /// </summary>
    [Test]
    public void WithoutServerLimitOnlyResetLimitApplies()
    {
        Assert.That(new ResetConfiguration { ResetLimit = 10 }.GetEffectiveResetLimit(Start), Is.EqualTo(10));
        Assert.That(new ResetConfiguration().GetEffectiveResetLimit(Start), Is.Null);
    }

    /// <summary>
    /// Tests the default experience multipliers by reset count.
    /// </summary>
    [TestCase(0, 540)]
    [TestCase(4, 540)]
    [TestCase(5, 432)]
    [TestCase(14, 324)]
    [TestCase(49, 126)]
    [TestCase(50, 108)]
    [TestCase(200, 108)]
    public void DefaultExperienceRateByResets(int resets, float expected)
    {
        Assert.That(ResetExperienceRateConfiguration.CreateDefault().GetMultiplier(resets), Is.EqualTo(expected));
    }

    private static ResetConfiguration CreateServerLimitConfiguration() => new()
    {
        ResetLimit = 55,
        ServerResetLimitStart = Start,
        ServerResetLimitInitial = 1,
        ServerResetLimitIncreasePerDay = 1,
    };
}
