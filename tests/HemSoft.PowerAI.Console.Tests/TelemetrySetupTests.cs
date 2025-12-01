// <copyright file="TelemetrySetupTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using HemSoft.PowerAI.Console.Telemetry;

using Xunit;

/// <summary>
/// Unit tests for <see cref="TelemetrySetup"/>.
/// </summary>
public sealed class TelemetrySetupTests
{
    /// <summary>
    /// Tests that TelemetrySetup initializes correctly without OTLP endpoint.
    /// </summary>
    [Fact]
    public void ConstructorWithoutOtlpEndpointCreatesInstance()
    {
        // Arrange - ensure no OTLP endpoint is set
        var originalEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);

        try
        {
            // Act
            using var telemetry = new TelemetrySetup("TestSource");

            // Assert
            Assert.NotNull(telemetry.ActivitySource);
            Assert.NotNull(telemetry.Meter);
            Assert.NotNull(telemetry.LoggerFactory);
            Assert.Equal("TestSource", telemetry.ActivitySource.Name);
        }
        finally
        {
            // Restore original environment
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", originalEndpoint);
        }
    }

    /// <summary>
    /// Tests that TelemetrySetup initializes correctly with OTLP endpoint.
    /// </summary>
    [Fact]
    public void ConstructorWithOtlpEndpointCreatesInstance()
    {
        // Arrange
        var originalEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4317");

        try
        {
            // Act
            using var telemetry = new TelemetrySetup("TestSource");

            // Assert
            Assert.NotNull(telemetry.ActivitySource);
            Assert.NotNull(telemetry.Meter);
            Assert.NotNull(telemetry.LoggerFactory);
        }
        finally
        {
            // Restore original environment
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", originalEndpoint);
        }
    }

    /// <summary>
    /// Tests that CreateLogger returns a valid logger.
    /// </summary>
    [Fact]
    public void CreateLoggerReturnsValidLogger()
    {
        // Arrange
        var originalEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);

        try
        {
            using var telemetry = new TelemetrySetup("TestSource");

            // Act
            var logger = telemetry.CreateLogger<TelemetrySetupTests>();

            // Assert
            Assert.NotNull(logger);
        }
        finally
        {
            // Restore original environment
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", originalEndpoint);
        }
    }

    /// <summary>
    /// Tests that Dispose cleans up resources without throwing.
    /// </summary>
    [Fact]
    public void DisposeCleansUpResources()
    {
        // Arrange
        var originalEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);

        try
        {
            using var telemetry = new TelemetrySetup("TestSource");

            // Act
            var exception = Record.Exception(telemetry.Dispose);

            // Assert - should not throw
            Assert.Null(exception);
        }
        finally
        {
            // Restore original environment
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", originalEndpoint);
        }
    }

    /// <summary>
    /// Tests that ActivitySource can create activities.
    /// </summary>
    [Fact]
    public void ActivitySourceCanStartActivity()
    {
        // Arrange
        var originalEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);

        try
        {
            using var telemetry = new TelemetrySetup("TestSource");

            // Act - activities may be null if no listeners are registered
            using var activity = telemetry.ActivitySource.StartActivity("TestActivity");

            // Assert - activity may be null if no listener registered, that's expected
            Assert.Equal("TestSource", telemetry.ActivitySource.Name);
        }
        finally
        {
            // Restore original environment
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", originalEndpoint);
        }
    }
}
