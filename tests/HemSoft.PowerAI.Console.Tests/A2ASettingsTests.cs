// <copyright file="A2ASettingsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Configuration;

/// <summary>
/// Tests for the <see cref="A2ASettings"/> class.
/// </summary>
public class A2ASettingsTests
{
    /// <summary>
    /// Verifies that the section name constant has the expected value.
    /// </summary>
    [Fact]
    public void SectionNameReturnsCorrectValue() =>
        Assert.Equal("A2A", A2ASettings.SectionName);

    /// <summary>
    /// Verifies that a new instance has a sensible default URL.
    /// </summary>
    [Fact]
    public void DefaultResearchAgentUrlHasDefaultValue()
    {
        // Arrange & Act
        var settings = new A2ASettings();

        // Assert
        Assert.NotNull(settings.DefaultResearchAgentUrl);
        Assert.StartsWith("http", settings.DefaultResearchAgentUrl.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that the DefaultResearchAgentUrl property can be set.
    /// </summary>
    [Fact]
    public void DefaultResearchAgentUrlCanBeSet()
    {
        // Arrange
        var settings = new A2ASettings();
        var customUrl = new Uri("http://custom-agent:8080/");

        // Act
        settings.DefaultResearchAgentUrl = customUrl;

        // Assert
        Assert.Equal(customUrl, settings.DefaultResearchAgentUrl);
    }

    /// <summary>
    /// Verifies that the ResearchAgentHostPort has a sensible default.
    /// </summary>
    [Fact]
    public void ResearchAgentHostPortHasDefaultValue()
    {
        // Arrange & Act
        var settings = new A2ASettings();

        // Assert
        Assert.Equal(5001, settings.ResearchAgentHostPort);
    }

    /// <summary>
    /// Verifies that the ResearchAgentHostPort property can be set.
    /// </summary>
    [Fact]
    public void ResearchAgentHostPortCanBeSet()
    {
        // Arrange
        var settings = new A2ASettings();
        const int CustomPort = 9999;

        // Act
        settings.ResearchAgentHostPort = CustomPort;

        // Assert
        Assert.Equal(CustomPort, settings.ResearchAgentHostPort);
    }
}
