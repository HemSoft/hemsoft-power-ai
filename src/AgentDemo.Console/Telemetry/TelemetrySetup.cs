// <copyright file="TelemetrySetup.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace AgentDemo.Console.Telemetry;

using System.Diagnostics;
using System.Diagnostics.Metrics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

/// <summary>
/// Configures OpenTelemetry for traces, metrics, and logs.
/// Exports to OTLP endpoint (Aspire Dashboard) when OTEL_EXPORTER_OTLP_ENDPOINT is set.
/// </summary>
internal sealed partial class TelemetrySetup : IDisposable
{
    private const string ServiceName = "AgentDemo.Console";
    private const string ServiceVersion = "1.0.0";

    private readonly TracerProvider? tracerProvider;
    private readonly MeterProvider? meterProvider;
    private readonly ServiceProvider serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetrySetup"/> class.
    /// </summary>
    /// <param name="sourceName">The source name for tracing and metrics.</param>
    public TelemetrySetup(string sourceName)
    {
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        var hasOtlpEndpoint = !string.IsNullOrWhiteSpace(otlpEndpoint);

        this.ActivitySource = new ActivitySource(sourceName, ServiceVersion);
        this.Meter = new Meter(sourceName, ServiceVersion);

        var resourceBuilder = ResourceBuilder
            .CreateDefault()
            .AddService(ServiceName, serviceVersion: ServiceVersion)
            .AddAttributes(new Dictionary<string, object>
            {
                ["service.instance.id"] = Environment.MachineName,
                ["deployment.environment"] = "development",
            });

        // Setup tracing
        var tracerBuilder = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddSource(sourceName)
            .AddSource("SpamFilterAgent")
            .AddSource("AgentDemo.GraphApi")
            .AddSource("AgentDemo.LLM")
            .AddSource("*Microsoft.Agents.AI")
            .AddHttpClientInstrumentation();

        if (hasOtlpEndpoint)
        {
            tracerBuilder.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
        }

        this.tracerProvider = tracerBuilder.Build();

        // Setup metrics
        var meterBuilder = Sdk.CreateMeterProviderBuilder()
            .SetResourceBuilder(resourceBuilder)
            .AddMeter(sourceName)
            .AddMeter("*Microsoft.Agents.AI")
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (hasOtlpEndpoint)
        {
            meterBuilder.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!));
        }

        this.meterProvider = meterBuilder.Build();

        // Setup logging with OpenTelemetry
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(resourceBuilder);
                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;

                if (hasOtlpEndpoint)
                {
                    options.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri(otlpEndpoint!));
                }
            });
        });

        this.serviceProvider = services.BuildServiceProvider();
        this.LoggerFactory = this.serviceProvider.GetRequiredService<ILoggerFactory>();

        if (hasOtlpEndpoint)
        {
            var logger = this.LoggerFactory.CreateLogger<TelemetrySetup>();
            LogOtlpConfigured(logger, otlpEndpoint!);
        }
    }

    /// <summary>
    /// Gets the activity source for creating spans.
    /// </summary>
    public ActivitySource ActivitySource { get; }

    /// <summary>
    /// Gets the meter for creating metrics.
    /// </summary>
    public Meter Meter { get; }

    /// <summary>
    /// Gets the logger factory.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Creates a logger for the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create a logger for.</typeparam>
    /// <returns>A logger instance.</returns>
    public ILogger<T> CreateLogger<T>() => this.LoggerFactory.CreateLogger<T>();

    /// <inheritdoc/>
    public void Dispose()
    {
        this.ActivitySource.Dispose();
        this.Meter.Dispose();
        this.tracerProvider?.Dispose();
        this.meterProvider?.Dispose();
        this.serviceProvider.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenTelemetry configured with OTLP endpoint: {Endpoint}")]
    private static partial void LogOtlpConfigured(ILogger logger, string endpoint);
}
