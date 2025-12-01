// <copyright file="EmailEvaluationTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Models;

/// <summary>
/// Tests for <see cref="EmailEvaluation"/>.
/// </summary>
public sealed class EmailEvaluationTests
{
    /// <summary>
    /// Verifies EmailEvaluation can be created with required properties.
    /// </summary>
    [Fact]
    public void CreateWithRequiredPropertiesSetsValues()
    {
        var evaluation = new EmailEvaluation
        {
            MessageId = "msg-123",
            Sender = "test@example.com",
            Subject = "Test Subject",
            Verdict = "Legitimate",
        };

        Assert.Equal("msg-123", evaluation.MessageId);
        Assert.Equal("test@example.com", evaluation.Sender);
        Assert.Equal("Test Subject", evaluation.Subject);
        Assert.Equal("Legitimate", evaluation.Verdict);
        Assert.Null(evaluation.Reason);
    }

    /// <summary>
    /// Verifies EmailEvaluation Reason property can be set.
    /// </summary>
    [Fact]
    public void CreateWithReasonSetsReasonValue()
    {
        var evaluation = new EmailEvaluation
        {
            MessageId = "msg-456",
            Sender = "spam@bad.com",
            Subject = "Win money now!",
            Verdict = "Known Spam",
            Reason = "Matched spam domain list",
        };

        Assert.Equal("Matched spam domain list", evaluation.Reason);
    }

    /// <summary>
    /// Verifies EmailEvaluation supports different verdict types.
    /// </summary>
    /// <param name="verdict">The verdict to test.</param>
    [Theory]
    [InlineData("Legitimate")]
    [InlineData("Known Spam")]
    [InlineData("Candidate")]
    [InlineData("Junked")]
    public void CreateWithDifferentVerdictsWorks(string verdict)
    {
        var evaluation = new EmailEvaluation
        {
            MessageId = "msg-test",
            Sender = "test@test.com",
            Subject = "Test",
            Verdict = verdict,
        };

        Assert.Equal(verdict, evaluation.Verdict);
    }

    /// <summary>
    /// Verifies record equality works correctly.
    /// </summary>
    [Fact]
    public void RecordEqualityWorksCorrectly()
    {
        var eval1 = new EmailEvaluation
        {
            MessageId = "msg-1",
            Sender = "a@b.com",
            Subject = "Test",
            Verdict = "Legitimate",
        };

        var eval2 = new EmailEvaluation
        {
            MessageId = "msg-1",
            Sender = "a@b.com",
            Subject = "Test",
            Verdict = "Legitimate",
        };

        var eval3 = new EmailEvaluation
        {
            MessageId = "msg-2",
            Sender = "a@b.com",
            Subject = "Test",
            Verdict = "Legitimate",
        };

        Assert.Equal(eval1, eval2);
        Assert.NotEqual(eval1, eval3);
    }

    /// <summary>
    /// Verifies record with expression creates new instance with changed value.
    /// </summary>
    [Fact]
    public void WithExpressionCreatesModifiedCopy()
    {
        var original = new EmailEvaluation
        {
            MessageId = "msg-orig",
            Sender = "test@example.com",
            Subject = "Original",
            Verdict = "Candidate",
        };

        var modified = original with { Verdict = "Legitimate", Reason = "Verified safe" };

        Assert.Equal("msg-orig", modified.MessageId);
        Assert.Equal("test@example.com", modified.Sender);
        Assert.Equal("Original", modified.Subject);
        Assert.Equal("Legitimate", modified.Verdict);
        Assert.Equal("Verified safe", modified.Reason);
        Assert.NotEqual(original, modified);
    }

    /// <summary>
    /// Verifies ToString returns meaningful representation.
    /// </summary>
    [Fact]
    public void ToStringReturnsReadableFormat()
    {
        var evaluation = new EmailEvaluation
        {
            MessageId = "msg-str",
            Sender = "test@example.com",
            Subject = "Test Subject",
            Verdict = "Legitimate",
        };

        var str = evaluation.ToString();

        Assert.Contains("EmailEvaluation", str, StringComparison.Ordinal);
        Assert.Contains("msg-str", str, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies GetHashCode is consistent for equal records.
    /// </summary>
    [Fact]
    public void GetHashCodeConsistentForEqualRecords()
    {
        var eval1 = new EmailEvaluation
        {
            MessageId = "msg-hash",
            Sender = "test@test.com",
            Subject = "Hash Test",
            Verdict = "Legitimate",
            Reason = "Test reason",
        };

        var eval2 = new EmailEvaluation
        {
            MessageId = "msg-hash",
            Sender = "test@test.com",
            Subject = "Hash Test",
            Verdict = "Legitimate",
            Reason = "Test reason",
        };

        Assert.Equal(eval1.GetHashCode(), eval2.GetHashCode());
    }
}
