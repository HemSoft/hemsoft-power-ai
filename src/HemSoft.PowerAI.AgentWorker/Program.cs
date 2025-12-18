// <copyright file="Program.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

using HemSoft.PowerAI.AgentWorker;
using HemSoft.PowerAI.AgentWorker.Configuration;
using HemSoft.PowerAI.Common.Configuration;
using HemSoft.PowerAI.Common.Services;

using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "HemSoft.PowerAI.AgentWorker";

var builder = Host.CreateApplicationBuilder(args);

// Configure Redis settings
var redisSettings = new RedisSettings();
builder.Configuration.GetSection(RedisSettings.SectionName).Bind(redisSettings);

// Configure AgentHost settings
builder.Services.Configure<AgentHostSettings>(
    builder.Configuration.GetSection(AgentHostSettings.SectionName));

// Register result storage service for large payloads
builder.Services.AddSingleton<IResultStorageService>(_ =>
    new RedisResultStorageService(redisSettings.ConnectionString));

// Register Redis broker as singleton with result storage (DI owns storage, not broker)
builder.Services.AddSingleton<IAgentTaskBroker>(sp =>
{
    var resultStorage = sp.GetRequiredService<IResultStorageService>();
    return new RedisAgentTaskBroker(redisSettings.ConnectionString, resultStorage, ownsResultStorage: false);
});

// Register the worker service
builder.Services.AddHostedService<AgentWorkerService>();

// Configure OpenTelemetry
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(ServiceName);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .SetResourceBuilder(resourceBuilder)
        .AddSource(ServiceName)
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .SetResourceBuilder(resourceBuilder)
        .AddMeter(ServiceName)
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resourceBuilder);
    options.AddOtlpExporter();
});

var host = builder.Build();

await host.RunAsync().ConfigureAwait(false);
