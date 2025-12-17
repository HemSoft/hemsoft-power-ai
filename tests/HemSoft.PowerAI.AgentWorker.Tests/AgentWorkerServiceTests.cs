// <copyright file="AgentWorkerServiceTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.AgentWorker.Tests;

using HemSoft.PowerAI.AgentWorker.Configuration;
using HemSoft.PowerAI.Common.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

/// <summary>
/// Unit tests for <see cref="AgentWorkerService"/>.
/// </summary>
public class AgentWorkerServiceTests
{
    /// <summary>
    /// Tests that constructor throws for null broker.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullBroker()
    {
        // Arrange - use NullLogger to avoid Moq issues with internal type generics
        var logger = NullLogger<AgentWorkerService>.Instance;
        var agentHostSettings = Options.Create(CreateValidAgentHostSettings());

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => CreateServiceForArgumentValidation(null!, logger, agentHostSettings, TimeProvider.System));
        Assert.Contains("broker", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that constructor throws for null logger.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullLogger()
    {
        // Arrange
        var broker = new Mock<IAgentTaskBroker>();
        var agentHostSettings = Options.Create(CreateValidAgentHostSettings());

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => CreateServiceForArgumentValidation(broker.Object, null!, agentHostSettings, TimeProvider.System));
        Assert.Contains("logger", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that constructor throws for null time provider.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullTimeProvider()
    {
        // Arrange
        var broker = new Mock<IAgentTaskBroker>();
        var logger = NullLogger<AgentWorkerService>.Instance;
        var agentHostSettings = Options.Create(CreateValidAgentHostSettings());

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => CreateServiceForArgumentValidation(broker.Object, logger, agentHostSettings, null!));
        Assert.Contains("timeProvider", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that constructor throws for null agent host settings.
    /// </summary>
    [Fact]
    public void ConstructorThrowsForNullAgentHostSettings()
    {
        // Arrange
        var broker = new Mock<IAgentTaskBroker>();
        var logger = NullLogger<AgentWorkerService>.Instance;

        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(
            () => CreateServiceForArgumentValidation(broker.Object, logger, null!, TimeProvider.System));
        Assert.Contains("agentHostSettings", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tests that constructor creates service without throwing when valid arguments provided.
    /// </summary>
    [Fact]
    public void ConstructorCreatesServiceWithValidArguments()
    {
        // Arrange
        var broker = new Mock<IAgentTaskBroker>();
        var logger = NullLogger<AgentWorkerService>.Instance;
        var agentHostSettings = Options.Create(CreateValidAgentHostSettings());

        // Act
        using var service = new AgentWorkerService(broker.Object, logger, agentHostSettings, TimeProvider.System);

        // Assert
        Assert.NotNull(service);
    }

    /// <summary>
    /// Creates a valid AgentHostSettings instance for testing.
    /// </summary>
    /// <returns>A valid AgentHostSettings configuration.</returns>
    private static AgentHostSettings CreateValidAgentHostSettings() =>
        new() { BaseUrl = new Uri("http://localhost:5001") };

    /// <summary>
    /// Helper method to create service for argument validation tests.
    /// Disposes the service if creation succeeds (which it won't in validation tests).
    /// </summary>
    /// <param name="broker">The task broker.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="agentHostSettings">The agent host settings.</param>
    /// <param name="timeProvider">The time provider.</param>
    private static void CreateServiceForArgumentValidation(
        IAgentTaskBroker broker,
        ILogger<AgentWorkerService> logger,
        IOptions<AgentHostSettings> agentHostSettings,
        TimeProvider timeProvider)
    {
        using var service = new AgentWorkerService(broker, logger, agentHostSettings, timeProvider);
    }
}
