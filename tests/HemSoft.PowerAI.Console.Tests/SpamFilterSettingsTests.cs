// <copyright file="SpamFilterSettingsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Configuration;

/// <summary>
/// Unit tests for <see cref="SpamFilterSettings"/>.
/// </summary>
public class SpamFilterSettingsTests
{
    /// <summary>
    /// Tests that default values are set correctly.
    /// </summary>
    [Fact]
    public void ConstructorSetsDefaultValues()
    {
        // Act
        var settings = new SpamFilterSettings();

        // Assert
        Assert.Equal(10, settings.BatchSize);
        Assert.Equal(30, settings.DelayBetweenBatchesSeconds);
        Assert.Equal("Data/SpamDomains.json", settings.SpamDomainsFilePath);
        Assert.Equal("Data/SpamCandidates.json", settings.SpamCandidatesFilePath);
    }

    /// <summary>
    /// Tests that the section name is correct.
    /// </summary>
    [Fact]
    public void SectionNameReturnsCorrectValue() =>
        Assert.Equal("SpamFilter", SpamFilterSettings.SectionName);

    /// <summary>
    /// Tests that properties can be set and retrieved.
    /// </summary>
    [Fact]
    public void PropertiesCanBeSetAndRetrieved()
    {
        // Arrange & Act
        var settings = new SpamFilterSettings
        {
            BatchSize = 20,
            DelayBetweenBatchesSeconds = 60,
            SpamDomainsFilePath = "custom/spam.json",
            SpamCandidatesFilePath = "custom/candidates.json",
        };

        // Assert
        Assert.Equal(20, settings.BatchSize);
        Assert.Equal(60, settings.DelayBetweenBatchesSeconds);
        Assert.Equal("custom/spam.json", settings.SpamDomainsFilePath);
        Assert.Equal("custom/candidates.json", settings.SpamCandidatesFilePath);
    }
}
