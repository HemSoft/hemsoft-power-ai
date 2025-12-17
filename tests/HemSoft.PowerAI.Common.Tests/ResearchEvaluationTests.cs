// <copyright file="ResearchEvaluationTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Tests;

using System.Collections.Immutable;
using System.Text.Json;

using HemSoft.PowerAI.Common.Models;

/// <summary>
/// Tests for <see cref="ResearchEvaluation"/> model.
/// </summary>
public sealed class ResearchEvaluationTests
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Verifies CreateDefault returns a satisfactory evaluation.
    /// </summary>
    [Fact]
    public void CreateDefaultReturnsSatisfactoryEvaluation()
    {
        // Act
        var result = ResearchEvaluation.CreateDefault();

        // Assert
        Assert.True(result.IsSatisfactory);
        Assert.Equal(7, result.QualityScore);
        Assert.Empty(result.Gaps);
        Assert.Empty(result.FollowUpQuestions);
        Assert.Null(result.RefinedQuery);
        Assert.Contains("parsing failed", result.Reasoning, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies constructor sets all properties correctly.
    /// </summary>
    [Fact]
    public void ConstructorSetsAllProperties()
    {
        // Arrange
        var gaps = ImmutableArray.Create("Gap 1", "Gap 2");
        var followUps = ImmutableArray.Create("Question 1");

        // Act
        var evaluation = new ResearchEvaluation(
            IsSatisfactory: false,
            QualityScore: 5,
            Gaps: gaps,
            FollowUpQuestions: followUps,
            RefinedQuery: "Refined query",
            Reasoning: "Test reasoning");

        // Assert
        Assert.False(evaluation.IsSatisfactory);
        Assert.Equal(5, evaluation.QualityScore);
        Assert.Equal(2, evaluation.Gaps.Length);
        Assert.Equal("Gap 1", evaluation.Gaps[0]);
        Assert.Single(evaluation.FollowUpQuestions);
        Assert.Equal("Refined query", evaluation.RefinedQuery);
        Assert.Equal("Test reasoning", evaluation.Reasoning);
    }

    /// <summary>
    /// Verifies serialization and deserialization round-trip.
    /// </summary>
    [Fact]
    public void SerializationRoundTrips()
    {
        // Arrange
        var original = new ResearchEvaluation(
            IsSatisfactory: true,
            QualityScore: 8,
            Gaps: ["Gap A"],
            FollowUpQuestions: ["Q1", "Q2"],
            RefinedQuery: null,
            Reasoning: "Good research");

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<ResearchEvaluation>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.IsSatisfactory, deserialized.IsSatisfactory);
        Assert.Equal(original.QualityScore, deserialized.QualityScore);
        Assert.Equal(original.Gaps.Length, deserialized.Gaps.Length);
        Assert.Equal(original.FollowUpQuestions.Length, deserialized.FollowUpQuestions.Length);
        Assert.Equal(original.RefinedQuery, deserialized.RefinedQuery);
        Assert.Equal(original.Reasoning, deserialized.Reasoning);
    }

    /// <summary>
    /// Verifies deserialization from JSON with camelCase property names.
    /// </summary>
    [Fact]
    public void DeserializationFromJsonWithCamelCaseWorks()
    {
        // Arrange - JSON matching EvaluatorAgent output format
        const string Json = """
            {
                "isSatisfactory": false,
                "qualityScore": 6,
                "gaps": ["Missing competitor analysis"],
                "followUpQuestions": ["What are the pricing tiers?"],
                "refinedQuery": "competitor pricing analysis software",
                "reasoning": "Research lacks depth in competitive landscape"
            }
            """;

        // Act
        var evaluation = JsonSerializer.Deserialize<ResearchEvaluation>(Json, CaseInsensitiveOptions);

        // Assert
        Assert.NotNull(evaluation);
        Assert.False(evaluation.IsSatisfactory);
        Assert.Equal(6, evaluation.QualityScore);
        Assert.Single(evaluation.Gaps);
        Assert.Equal("Missing competitor analysis", evaluation.Gaps[0]);
        Assert.Single(evaluation.FollowUpQuestions);
        Assert.Equal("competitor pricing analysis software", evaluation.RefinedQuery);
    }

    /// <summary>
    /// Verifies deserialization with empty arrays.
    /// </summary>
    [Fact]
    public void DeserializationWithEmptyArraysWorks()
    {
        // Arrange
        const string Json = """
            {
                "isSatisfactory": true,
                "qualityScore": 9,
                "gaps": [],
                "followUpQuestions": [],
                "refinedQuery": null,
                "reasoning": "Excellent comprehensive research"
            }
            """;

        // Act
        var evaluation = JsonSerializer.Deserialize<ResearchEvaluation>(Json, CaseInsensitiveOptions);

        // Assert
        Assert.NotNull(evaluation);
        Assert.True(evaluation.IsSatisfactory);
        Assert.Equal(9, evaluation.QualityScore);
        Assert.Empty(evaluation.Gaps);
        Assert.Empty(evaluation.FollowUpQuestions);
        Assert.Null(evaluation.RefinedQuery);
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Fact]
    public void RecordEqualityWorksCorrectly()
    {
        // Arrange
        var eval1 = new ResearchEvaluation(
            IsSatisfactory: true,
            QualityScore: 7,
            Gaps: [],
            FollowUpQuestions: [],
            RefinedQuery: null,
            Reasoning: "Good");

        var eval2 = new ResearchEvaluation(
            IsSatisfactory: true,
            QualityScore: 7,
            Gaps: [],
            FollowUpQuestions: [],
            RefinedQuery: null,
            Reasoning: "Good");

        var eval3 = new ResearchEvaluation(
            IsSatisfactory: false,
            QualityScore: 5,
            Gaps: [],
            FollowUpQuestions: [],
            RefinedQuery: "new query",
            Reasoning: "Needs work");

        // Assert
        Assert.Equal(eval1, eval2);
        Assert.NotEqual(eval1, eval3);
    }
}
