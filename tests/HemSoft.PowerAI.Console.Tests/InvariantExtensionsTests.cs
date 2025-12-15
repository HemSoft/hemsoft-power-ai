// <copyright file="InvariantExtensionsTests.cs" company="HemSoft">
// Copyright © 2025 HemSoft
// </copyright>

namespace HemSoft.PowerAI.Console.Tests;

using System.Globalization;

using HemSoft.PowerAI.Console.Extensions;

using Xunit;

/// <summary>
/// Unit tests for <see cref="InvariantExtensions"/>.
/// </summary>
public sealed class InvariantExtensionsTests
{
    /// <summary>
    /// Tests that int ToInvariant returns correct string.
    /// </summary>
    [Fact]
    public void IntToInvariantReturnsCorrectString()
    {
        // Arrange
        const int value = 12345;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("12345", result);
    }

    /// <summary>
    /// Tests that int ToInvariant handles negative numbers.
    /// </summary>
    [Fact]
    public void IntToInvariantHandlesNegativeNumbers()
    {
        // Arrange
        const int value = -9876;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-9876", result);
    }

    /// <summary>
    /// Tests that int ToInvariant handles zero.
    /// </summary>
    [Fact]
    public void IntToInvariantHandlesZero()
    {
        // Arrange
        const int value = 0;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("0", result);
    }

    /// <summary>
    /// Tests that int ToInvariant handles max value.
    /// </summary>
    [Fact]
    public void IntToInvariantHandlesMaxValue()
    {
        // Arrange
        const int value = int.MaxValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("2147483647", result);
    }

    /// <summary>
    /// Tests that int ToInvariant handles min value.
    /// </summary>
    [Fact]
    public void IntToInvariantHandlesMinValue()
    {
        // Arrange
        const int value = int.MinValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-2147483648", result);
    }

    /// <summary>
    /// Tests that int ToInvariant with format returns formatted string.
    /// </summary>
    [Fact]
    public void IntToInvariantWithFormatReturnsFormattedString()
    {
        // Arrange
        const int value = 42;

        // Act
        var result = value.ToInvariant("D5");

        // Assert
        Assert.Equal("00042", result);
    }

    /// <summary>
    /// Tests that int ToInvariant with hex format returns correct string.
    /// </summary>
    [Fact]
    public void IntToInvariantWithHexFormatReturnsCorrectString()
    {
        // Arrange
        const int value = 255;

        // Act
        var result = value.ToInvariant("X");

        // Assert
        Assert.Equal("FF", result);
    }

    /// <summary>
    /// Tests that long ToInvariant returns correct string.
    /// </summary>
    [Fact]
    public void LongToInvariantReturnsCorrectString()
    {
        // Arrange
        const long value = 9_876_543_210L;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("9876543210", result);
    }

    /// <summary>
    /// Tests that long ToInvariant handles negative numbers.
    /// </summary>
    [Fact]
    public void LongToInvariantHandlesNegativeNumbers()
    {
        // Arrange
        const long value = -9_876_543_210L;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-9876543210", result);
    }

    /// <summary>
    /// Tests that long ToInvariant handles zero.
    /// </summary>
    [Fact]
    public void LongToInvariantHandlesZero()
    {
        // Arrange
        const long value = 0L;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("0", result);
    }

    /// <summary>
    /// Tests that long ToInvariant handles max value.
    /// </summary>
    [Fact]
    public void LongToInvariantHandlesMaxValue()
    {
        // Arrange
        const long value = long.MaxValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("9223372036854775807", result);
    }

    /// <summary>
    /// Tests that long ToInvariant handles min value.
    /// </summary>
    [Fact]
    public void LongToInvariantHandlesMinValue()
    {
        // Arrange
        const long value = long.MinValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-9223372036854775808", result);
    }

    /// <summary>
    /// Tests that long ToInvariant with format returns formatted string.
    /// </summary>
    [Fact]
    public void LongToInvariantWithFormatReturnsFormattedString()
    {
        // Arrange
        const long value = 123L;

        // Act
        var result = value.ToInvariant("D10");

        // Assert
        Assert.Equal("0000000123", result);
    }

    /// <summary>
    /// Tests that long ToInvariant with number format returns correct string.
    /// </summary>
    [Fact]
    public void LongToInvariantWithNumberFormatReturnsCorrectString()
    {
        // Arrange
        const long value = 1_234_567_890L;

        // Act
        var result = value.ToInvariant("N0");

        // Assert
        Assert.Equal("1,234,567,890", result);
    }

    /// <summary>
    /// Tests that double ToInvariant returns correct string.
    /// </summary>
    [Fact]
    public void DoubleToInvariantReturnsCorrectString()
    {
        // Arrange
        const double value = 123.456;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("123.456", result);
    }

    /// <summary>
    /// Tests that double ToInvariant handles negative numbers.
    /// </summary>
    [Fact]
    public void DoubleToInvariantHandlesNegativeNumbers()
    {
        // Arrange
        const double value = -987.654;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-987.654", result);
    }

    /// <summary>
    /// Tests that double ToInvariant handles zero.
    /// </summary>
    [Fact]
    public void DoubleToInvariantHandlesZero()
    {
        // Arrange
        const double value = 0.0;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("0", result);
    }

    /// <summary>
    /// Tests that double ToInvariant handles very small numbers.
    /// </summary>
    [Fact]
    public void DoubleToInvariantHandlesVerySmallNumbers()
    {
        // Arrange
        const double value = 0.000001;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("1E-06", result);
    }

    /// <summary>
    /// Tests that double ToInvariant handles very large numbers.
    /// </summary>
    [Fact]
    public void DoubleToInvariantHandlesVeryLargeNumbers()
    {
        // Arrange
        const double value = 1e15;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("1000000000000000", result);
    }

    /// <summary>
    /// Tests that double ToInvariant with format returns formatted string.
    /// </summary>
    [Fact]
    public void DoubleToInvariantWithFormatReturnsFormattedString()
    {
        // Arrange
        const double value = 123.456789;

        // Act
        var result = value.ToInvariant("F2");

        // Assert
        Assert.Equal("123.46", result);
    }

    /// <summary>
    /// Tests that double ToInvariant with percent format returns correct string.
    /// </summary>
    [Fact]
    public void DoubleToInvariantWithPercentFormatReturnsCorrectString()
    {
        // Arrange
        const double value = 0.75;

        // Act
        var result = value.ToInvariant("P0");

        // Assert
        Assert.Equal("75 %", result);
    }

    /// <summary>
    /// Tests that decimal ToInvariant returns correct string.
    /// </summary>
    [Fact]
    public void DecimalToInvariantReturnsCorrectString()
    {
        // Arrange
        const decimal value = 123.456m;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("123.456", result);
    }

    /// <summary>
    /// Tests that decimal ToInvariant handles negative numbers.
    /// </summary>
    [Fact]
    public void DecimalToInvariantHandlesNegativeNumbers()
    {
        // Arrange
        const decimal value = -987.654m;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-987.654", result);
    }

    /// <summary>
    /// Tests that decimal ToInvariant handles zero.
    /// </summary>
    [Fact]
    public void DecimalToInvariantHandlesZero()
    {
        // Arrange
        const decimal value = 0m;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("0", result);
    }

    /// <summary>
    /// Tests that decimal ToInvariant handles max value.
    /// </summary>
    [Fact]
    public void DecimalToInvariantHandlesMaxValue()
    {
        // Arrange
        const decimal value = decimal.MaxValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("79228162514264337593543950335", result);
    }

    /// <summary>
    /// Tests that decimal ToInvariant handles min value.
    /// </summary>
    [Fact]
    public void DecimalToInvariantHandlesMinValue()
    {
        // Arrange
        const decimal value = decimal.MinValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-79228162514264337593543950335", result);
    }

    /// <summary>
    /// Tests that decimal ToInvariant with format returns formatted string.
    /// </summary>
    [Fact]
    public void DecimalToInvariantWithFormatReturnsFormattedString()
    {
        // Arrange
        const decimal value = 123.456789m;

        // Act
        var result = value.ToInvariant("F3");

        // Assert
        Assert.Equal("123.457", result);
    }

    /// <summary>
    /// Tests that decimal ToInvariant with currency format returns correct string.
    /// </summary>
    [Fact]
    public void DecimalToInvariantWithCurrencyFormatReturnsCorrectString()
    {
        // Arrange
        const decimal value = 1234.56m;

        // Act
        var result = value.ToInvariant("C");

        // Assert
        Assert.Equal("¤1,234.56", result);
    }

    /// <summary>
    /// Tests that TimeSpan ToInvariant returns correct string.
    /// </summary>
    [Fact]
    public void TimeSpanToInvariantReturnsCorrectString()
    {
        // Arrange
        var value = new TimeSpan(1, 2, 3, 4, 5);

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("1.02:03:04.0050000", result);
    }

    /// <summary>
    /// Tests that TimeSpan ToInvariant handles zero.
    /// </summary>
    [Fact]
    public void TimeSpanToInvariantHandlesZero()
    {
        // Arrange
        var value = TimeSpan.Zero;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("00:00:00", result);
    }

    /// <summary>
    /// Tests that TimeSpan ToInvariant handles negative values.
    /// </summary>
    [Fact]
    public void TimeSpanToInvariantHandlesNegativeValues()
    {
        // Arrange
        var value = TimeSpan.FromHours(-5);

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.Equal("-05:00:00", result);
    }

    /// <summary>
    /// Tests that TimeSpan ToInvariant handles max value.
    /// </summary>
    [Fact]
    public void TimeSpanToInvariantHandlesMaxValue()
    {
        // Arrange
        var value = TimeSpan.MaxValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Tests that TimeSpan ToInvariant handles min value.
    /// </summary>
    [Fact]
    public void TimeSpanToInvariantHandlesMinValue()
    {
        // Arrange
        var value = TimeSpan.MinValue;

        // Act
        var result = value.ToInvariant();

        // Assert
        Assert.NotEmpty(result);
    }

    /// <summary>
    /// Tests that TimeSpan ToInvariant with format returns formatted string.
    /// </summary>
    [Fact]
    public void TimeSpanToInvariantWithFormatReturnsFormattedString()
    {
        // Arrange
        var value = new TimeSpan(2, 30, 45);

        // Act
        var result = value.ToInvariant(@"hh\:mm\:ss");

        // Assert
        Assert.Equal("02:30:45", result);
    }

    /// <summary>
    /// Tests that TimeSpan ToInvariant with custom format returns correct string.
    /// </summary>
    [Fact]
    public void TimeSpanToInvariantWithCustomFormatReturnsCorrectString()
    {
        // Arrange
        var value = TimeSpan.FromMinutes(90);

        // Act
        var result = value.ToInvariant(@"h\:mm");

        // Assert
        Assert.Equal("1:30", result);
    }

    /// <summary>
    /// Tests that int ToInvariant is culture invariant.
    /// </summary>
    [Fact]
    public void IntToInvariantIsCultureInvariant()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        const int value = 1_234_567;

        try
        {
            // Act - test with different cultures
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var resultGerman = value.ToInvariant();

            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var resultFrench = value.ToInvariant();

            // Assert - both should be the same (invariant)
            Assert.Equal("1234567", resultGerman);
            Assert.Equal("1234567", resultFrench);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Tests that double ToInvariant is culture invariant.
    /// </summary>
    [Fact]
    public void DoubleToInvariantIsCultureInvariant()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        const double value = 1234.56;

        try
        {
            // Act - test with different cultures (German uses comma for decimal)
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var resultGerman = value.ToInvariant();

            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
            var resultFrench = value.ToInvariant();

            // Assert - both should use period as decimal separator (invariant)
            Assert.Equal("1234.56", resultGerman);
            Assert.Equal("1234.56", resultFrench);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Tests that decimal ToInvariant is culture invariant.
    /// </summary>
    [Fact]
    public void DecimalToInvariantIsCultureInvariant()
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        const decimal value = 9876.54m;

        try
        {
            // Act
            CultureInfo.CurrentCulture = new CultureInfo("es-ES");
            var resultSpanish = value.ToInvariant();

            CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
            var resultJapanese = value.ToInvariant();

            // Assert
            Assert.Equal("9876.54", resultSpanish);
            Assert.Equal("9876.54", resultJapanese);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
