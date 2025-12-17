// <copyright file="ResearchIterationStateTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Common.Tests;

using System.Globalization;

using HemSoft.PowerAI.Common.Models;

/// <summary>
/// Tests for <see cref="ResearchIterationState"/> model.
/// </summary>
public sealed class ResearchIterationStateTests
{
    private static readonly TimeProvider FakeTime = TimeProvider.System;

    /// <summary>
    /// Verifies constructor sets original query.
    /// </summary>
    [Fact]
    public void ConstructorSetsOriginalQuery()
    {
        // Arrange & Act
        var state = new ResearchIterationState("test query", FakeTime);

        // Assert
        Assert.Equal("test query", state.OriginalQuery);
        Assert.False(state.IsComplete);
        Assert.Null(state.FinalSynthesis);
        Assert.Empty(state.Iterations);
        Assert.Equal(0, state.CurrentIteration);
    }

    /// <summary>
    /// Verifies AddIteration increments current iteration count.
    /// </summary>
    [Fact]
    public void AddIterationIncrementsCurrentIteration()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime);
        var evaluation = CreateTestEvaluation();

        // Act
        state.AddIteration("search query 1", "findings 1", evaluation);

        // Assert
        Assert.Equal(1, state.CurrentIteration);
        Assert.Single(state.Iterations);
    }

    /// <summary>
    /// Verifies AddIteration stores iteration data correctly.
    /// </summary>
    [Fact]
    public void AddIterationStoresIterationData()
    {
        // Arrange
        var state = new ResearchIterationState("original", FakeTime);
        var evaluation = CreateTestEvaluation(qualityScore: 6);

        // Act
        state.AddIteration("iteration query", "iteration findings", evaluation);

        // Assert
        var iteration = state.Iterations[0];
        Assert.Equal(1, iteration.IterationNumber);
        Assert.Equal("iteration query", iteration.Query);
        Assert.Equal("iteration findings", iteration.Findings);
        Assert.Equal(6, iteration.Evaluation.QualityScore);
    }

    /// <summary>
    /// Verifies LatestEvaluation returns null when no iterations exist.
    /// </summary>
    [Fact]
    public void LatestEvaluationReturnsNullWhenNoIterations()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime);

        // Act & Assert
        Assert.Null(state.LatestEvaluation);
    }

    /// <summary>
    /// Verifies LatestEvaluation returns most recent evaluation.
    /// </summary>
    [Fact]
    public void LatestEvaluationReturnsMostRecent()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime);
        var eval1 = CreateTestEvaluation(qualityScore: 5);
        var eval2 = CreateTestEvaluation(qualityScore: 7);
        var eval3 = CreateTestEvaluation(qualityScore: 9);

        // Act
        state.AddIteration("q1", "f1", eval1);
        state.AddIteration("q2", "f2", eval2);
        state.AddIteration("q3", "f3", eval3);

        // Assert
        Assert.Equal(9, state.LatestEvaluation?.QualityScore);
    }

    /// <summary>
    /// Verifies AllFindings concatenates all iterations.
    /// </summary>
    [Fact]
    public void AllFindingsConcatenatesIterations()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime);
        var evaluation = CreateTestEvaluation();

        state.AddIteration("query 1", "findings one", evaluation);
        state.AddIteration("query 2", "findings two", evaluation);

        // Act
        var allFindings = state.AllFindings;

        // Assert
        Assert.Contains("Iteration 1", allFindings, StringComparison.Ordinal);
        Assert.Contains("findings one", allFindings, StringComparison.Ordinal);
        Assert.Contains("Iteration 2", allFindings, StringComparison.Ordinal);
        Assert.Contains("findings two", allFindings, StringComparison.Ordinal);
        Assert.Contains("---", allFindings, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies IsComplete property can be set.
    /// </summary>
    [Fact]
    public void IsCompleteCanBeSet()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime) { IsComplete = true };

        // Assert
        Assert.True(state.IsComplete);
    }

    /// <summary>
    /// Verifies FinalSynthesis property can be set.
    /// </summary>
    [Fact]
    public void FinalSynthesisCanBeSet()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime)
        {
            FinalSynthesis = "Final synthesized result",
        };

        // Assert
        Assert.Equal("Final synthesized result", state.FinalSynthesis);
    }

    /// <summary>
    /// Verifies Iterations is a read-only collection.
    /// </summary>
    [Fact]
    public void IterationsIsReadOnly()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime);
        var evaluation = CreateTestEvaluation();
        state.AddIteration("q", "f", evaluation);

        // Act & Assert - Verify it's a read-only collection
        var iterations = state.Iterations;
        Assert.IsType<System.Collections.ObjectModel.ReadOnlyCollection<ResearchIteration>>(iterations);
    }

    /// <summary>
    /// Verifies multiple iterations maintain order.
    /// </summary>
    [Fact]
    public void MultipleIterationsMaintainOrder()
    {
        // Arrange
        var state = new ResearchIterationState("query", FakeTime);
        var evaluation = CreateTestEvaluation();

        // Act
        for (var i = 1; i <= 5; i++)
        {
            state.AddIteration(
                string.Create(CultureInfo.InvariantCulture, $"query {i}"),
                string.Create(CultureInfo.InvariantCulture, $"findings {i}"),
                evaluation);
        }

        // Assert
        Assert.Equal(5, state.CurrentIteration);
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(i + 1, state.Iterations[i].IterationNumber);
            Assert.Equal(string.Create(CultureInfo.InvariantCulture, $"query {i + 1}"), state.Iterations[i].Query);
        }
    }

    private static ResearchEvaluation CreateTestEvaluation(
        bool isSatisfactory = true,
        int qualityScore = 7) =>
        new(
            IsSatisfactory: isSatisfactory,
            QualityScore: qualityScore,
            Gaps: [],
            FollowUpQuestions: [],
            RefinedQuery: null,
            Reasoning: "Test evaluation");
}
