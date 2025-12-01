// <copyright file="HumanReviewModelTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Text.Json;

using HemSoft.PowerAI.Console.Models;

/// <summary>
/// Unit tests for HumanReview model classes.
/// </summary>
public class HumanReviewModelTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Tests that HumanReviewFile initializes with empty Domains collection.
    /// </summary>
    [Fact]
    public void HumanReviewFileInitializesWithEmptyDomainsCollection()
    {
        // Act
        var file = new HumanReviewFile();

        // Assert
        Assert.NotNull(file.Domains);
        Assert.Empty(file.Domains);
    }

    /// <summary>
    /// Tests that HumanReviewFile Domains can be set to a collection.
    /// </summary>
    [Fact]
    public void HumanReviewFileDomainsCanBeSet()
    {
        // Arrange
        var file = new HumanReviewFile();
        var domain = new HumanReviewDomain { Domain = "TEST.COM" };

        // Act
        file.Domains = [domain];

        // Assert
        Assert.Single(file.Domains);
        Assert.Equal("TEST.COM", file.Domains[0].Domain);
    }

    /// <summary>
    /// Tests that HumanReviewFile serializes and deserializes correctly.
    /// </summary>
    [Fact]
    public void HumanReviewFileSerializesAndDeserializesCorrectly()
    {
        // Arrange
        var file = new HumanReviewFile
        {
            Domains =
            [
                new HumanReviewDomain
                {
                    Domain = "SPAM.COM",
                    EmailCount = 5,
                    FirstSeen = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LastSeen = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                    Samples =
                    [
                        new HumanReviewSample
                        {
                            MessageId = "msg1",
                            Sender = "spammer@spam.com",
                            Subject = "Win money!",
                            Reason = "Suspicious",
                        },
                    ],
                },
            ],
        };

        // Act
        var json = JsonSerializer.Serialize(file, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<HumanReviewFile>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Single(deserialized.Domains);
        Assert.Equal("SPAM.COM", deserialized.Domains[0].Domain);
        Assert.Equal(5, deserialized.Domains[0].EmailCount);
    }

    /// <summary>
    /// Tests that HumanReviewDomain initializes with default values.
    /// </summary>
    [Fact]
    public void HumanReviewDomainInitializesWithDefaultValues()
    {
        // Act
        var domain = new HumanReviewDomain { Domain = "TEST.COM" };

        // Assert
        Assert.Equal("TEST.COM", domain.Domain);
        Assert.Equal(0, domain.EmailCount);
        Assert.NotNull(domain.Samples);
        Assert.Empty(domain.Samples);
        Assert.True(domain.FirstSeen <= DateTime.UtcNow);
        Assert.True(domain.LastSeen <= DateTime.UtcNow);
    }

    /// <summary>
    /// Tests that HumanReviewDomain properties can be set.
    /// </summary>
    [Fact]
    public void HumanReviewDomainPropertiesCanBeSet()
    {
        // Arrange
        var firstSeen = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastSeen = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var domain = new HumanReviewDomain
        {
            Domain = "TEST.COM",
            EmailCount = 10,
            FirstSeen = firstSeen,
            LastSeen = lastSeen,
            Samples =
            [
                new HumanReviewSample
                {
                    MessageId = "msg1",
                    Sender = "a@test.com",
                    Subject = "Test",
                },
            ],
        };

        // Assert
        Assert.Equal("TEST.COM", domain.Domain);
        Assert.Equal(10, domain.EmailCount);
        Assert.Equal(firstSeen, domain.FirstSeen);
        Assert.Equal(lastSeen, domain.LastSeen);
        Assert.Single(domain.Samples);
    }

    /// <summary>
    /// Tests that HumanReviewDomain Samples collection supports add operations.
    /// </summary>
    [Fact]
    public void HumanReviewDomainSamplesSupportsAddOperations()
    {
        // Arrange
        var domain = new HumanReviewDomain { Domain = "TEST.COM" };

        // Act
        domain.Samples.Add(new HumanReviewSample
        {
            MessageId = "msg1",
            Sender = "a@test.com",
            Subject = "Subject 1",
        });
        domain.Samples.Add(new HumanReviewSample
        {
            MessageId = "msg2",
            Sender = "b@test.com",
            Subject = "Subject 2",
        });

        // Assert
        Assert.Equal(2, domain.Samples.Count);
    }

    /// <summary>
    /// Tests that HumanReviewSample initializes with required properties.
    /// </summary>
    [Fact]
    public void HumanReviewSampleInitializesWithRequiredProperties()
    {
        // Act
        var sample = new HumanReviewSample
        {
            MessageId = "msg123",
            Sender = "test@example.com",
            Subject = "Test Subject",
        };

        // Assert
        Assert.Equal("msg123", sample.MessageId);
        Assert.Equal("test@example.com", sample.Sender);
        Assert.Equal("Test Subject", sample.Subject);
        Assert.Null(sample.Reason);
    }

    /// <summary>
    /// Tests that HumanReviewSample Reason property can be set.
    /// </summary>
    [Fact]
    public void HumanReviewSampleReasonCanBeSet()
    {
        // Act
        var sample = new HumanReviewSample
        {
            MessageId = "msg123",
            Sender = "test@example.com",
            Subject = "Test Subject",
            Reason = "Suspicious content",
        };

        // Assert
        Assert.Equal("Suspicious content", sample.Reason);
    }

    /// <summary>
    /// Tests that HumanReviewSample serializes and deserializes correctly.
    /// </summary>
    [Fact]
    public void HumanReviewSampleSerializesAndDeserializesCorrectly()
    {
        // Arrange
        var sample = new HumanReviewSample
        {
            MessageId = "msg123",
            Sender = "test@example.com",
            Subject = "Test Subject",
            Reason = "Suspicious",
        };

        // Act
        var json = JsonSerializer.Serialize(sample, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<HumanReviewSample>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(sample.MessageId, deserialized.MessageId);
        Assert.Equal(sample.Sender, deserialized.Sender);
        Assert.Equal(sample.Subject, deserialized.Subject);
        Assert.Equal(sample.Reason, deserialized.Reason);
    }

    /// <summary>
    /// Tests that HumanReviewSample with null Reason serializes correctly.
    /// </summary>
    [Fact]
    public void HumanReviewSampleWithNullReasonSerializesCorrectly()
    {
        // Arrange
        var sample = new HumanReviewSample
        {
            MessageId = "msg123",
            Sender = "test@example.com",
            Subject = "Test Subject",
            Reason = null,
        };

        // Act
        var json = JsonSerializer.Serialize(sample, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<HumanReviewSample>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Reason);
    }

    /// <summary>
    /// Tests that HumanReviewDomain serializes and deserializes correctly.
    /// </summary>
    [Fact]
    public void HumanReviewDomainSerializesAndDeserializesCorrectly()
    {
        // Arrange
        var domain = new HumanReviewDomain
        {
            Domain = "TEST.COM",
            EmailCount = 3,
            FirstSeen = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            LastSeen = new DateTime(2025, 1, 5, 12, 0, 0, DateTimeKind.Utc),
            Samples =
            [
                new HumanReviewSample
                {
                    MessageId = "msg1",
                    Sender = "a@test.com",
                    Subject = "Subject 1",
                    Reason = "Reason 1",
                },
                new HumanReviewSample
                {
                    MessageId = "msg2",
                    Sender = "b@test.com",
                    Subject = "Subject 2",
                },
            ],
        };

        // Act
        var json = JsonSerializer.Serialize(domain, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<HumanReviewDomain>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("TEST.COM", deserialized.Domain);
        Assert.Equal(3, deserialized.EmailCount);
        Assert.Equal(2, deserialized.Samples.Count);
    }

    /// <summary>
    /// Tests that HumanReviewFile handles empty domains array.
    /// </summary>
    [Fact]
    public void HumanReviewFileHandlesEmptyDomainsArray()
    {
        // Arrange
        const string json = """{"domains":[]}""";

        // Act
        var deserialized = JsonSerializer.Deserialize<HumanReviewFile>(json, JsonOptions);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Empty(deserialized.Domains);
    }

    /// <summary>
    /// Tests that HumanReviewDomain Samples can be replaced.
    /// </summary>
    [Fact]
    public void HumanReviewDomainSamplesCanBeReplaced()
    {
        // Arrange
        var domain = new HumanReviewDomain { Domain = "TEST.COM" };
        domain.Samples.Add(new HumanReviewSample
        {
            MessageId = "old",
            Sender = "old@test.com",
            Subject = "Old",
        });

        // Act
        domain.Samples =
        [
            new HumanReviewSample
            {
                MessageId = "new",
                Sender = "new@test.com",
                Subject = "New",
            },
        ];

        // Assert
        Assert.Single(domain.Samples);
        Assert.Equal("new", domain.Samples[0].MessageId);
    }
}
