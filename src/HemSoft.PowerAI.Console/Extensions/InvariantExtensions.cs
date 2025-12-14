// <copyright file="InvariantExtensions.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Extensions;

using System.Globalization;

/// <summary>
/// Extension methods for culture-invariant string conversions.
/// </summary>
internal static class InvariantExtensions
{
    /// <summary>
    /// Converts the integer to a culture-invariant string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this int value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the integer to a culture-invariant string with the specified format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this int value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the long to a culture-invariant string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this long value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the long to a culture-invariant string with the specified format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this long value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the double to a culture-invariant string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this double value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the double to a culture-invariant string with the specified format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this double value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the decimal to a culture-invariant string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this decimal value) =>
        value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the decimal to a culture-invariant string with the specified format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this decimal value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the TimeSpan to a culture-invariant string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this TimeSpan value) =>
        value.ToString("c", CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the TimeSpan to a culture-invariant string with the specified format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="format">The format string.</param>
    /// <returns>The culture-invariant string representation.</returns>
    public static string ToInvariant(this TimeSpan value, string format) =>
        value.ToString(format, CultureInfo.InvariantCulture);
}
