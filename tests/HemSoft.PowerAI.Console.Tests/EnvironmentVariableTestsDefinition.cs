// <copyright file="EnvironmentVariableTestsDefinition.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Collection definition for tests that modify environment variables.
/// Tests in this collection run sequentially to avoid race conditions.
/// </summary>
/// <remarks>
/// Must be public per xUnit requirements (xUnit1027).
/// Suppressions are necessary due to xUnit's design constraints.
/// </remarks>
[CollectionDefinition("EnvironmentVariableTests", DisableParallelization = true)]
[SuppressMessage(
    "CA1515",
    "CA1515:Consider making public types internal",
    Justification = "xUnit requires public collection definition classes")]
public sealed class EnvironmentVariableTestsDefinition;
