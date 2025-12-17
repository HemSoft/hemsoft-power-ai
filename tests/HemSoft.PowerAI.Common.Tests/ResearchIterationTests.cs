// <copyright file="ResearchIterationTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Tests;

using HemSoft.PowerAI.Common.Models;

/// <summary>
/// Tests for <see cref="ResearchIteration"/> record.
/// </summary>
public sealed class ResearchIterationTests
{
    /// <summary>
    /// Verifies constructor sets all properties correctly.
    /// </summary>
    [Fact]
    public void ConstructorSetsAllProperties()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evaluation = new ResearchEvaluation(
            IsSatisfactory: true,
            QualityScore: 8,
            Gaps: [],
            FollowUpQuestions: [],
            RefinedQuery: null,
            Reasoning: "Good");

        // Act
        var iteration = new ResearchIteration(
            IterationNumber: 1,
            Query: "test query",
            Findings: "test findings",
            Evaluation: evaluation,
            Timestamp: timestamp);

        // Assert
        Assert.Equal(1, iteration.IterationNumber);
        Assert.Equal("test query", iteration.Query);
        Assert.Equal("test findings", iteration.Findings);
        Assert.Equal(evaluation, iteration.Evaluation);
        Assert.Equal(timestamp, iteration.Timestamp);
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Fact]
    public void RecordEqualityWorksCorrectly()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evaluation = ResearchEvaluation.CreateDefault();

        var iter1 = new ResearchIteration(1, "query", "findings", evaluation, timestamp);
        var iter2 = new ResearchIteration(1, "query", "findings", evaluation, timestamp);
        var iter3 = new ResearchIteration(2, "query", "findings", evaluation, timestamp);

        // Assert
        Assert.Equal(iter1, iter2);
        Assert.NotEqual(iter1, iter3);
    }

    /// <summary>
    /// Verifies record with expression creates a new instance.
    /// </summary>
    [Fact]
    public void RecordWithCreatesNewInstance()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evaluation = ResearchEvaluation.CreateDefault();
        var original = new ResearchIteration(1, "query", "findings", evaluation, timestamp);

        // Act
        var modified = original with { IterationNumber = 2 };

        // Assert
        Assert.Equal(1, original.IterationNumber);
        Assert.Equal(2, modified.IterationNumber);
        Assert.Equal(original.Query, modified.Query);
    }

    /// <summary>
    /// Verifies record ToString contains relevant information.
    /// </summary>
    [Fact]
    public void RecordToStringContainsRelevantInfo()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var evaluation = ResearchEvaluation.CreateDefault();
        var iteration = new ResearchIteration(3, "my query", "my findings", evaluation, timestamp);

        // Act
        var str = iteration.ToString();

        // Assert
        Assert.Contains("3", str, StringComparison.Ordinal);
        Assert.Contains("my query", str, StringComparison.Ordinal);
    }
}
