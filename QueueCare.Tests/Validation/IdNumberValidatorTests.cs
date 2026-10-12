using System;
using QueueCare.Validation;
using Xunit;

namespace QueueCare.Tests.Validation;

// Requirement FR-02 (test case TC-002 in the test workbook).
// The date is fixed so the tests give the same result on any day.
public class IdNumberValidatorTests
{
    private static readonly DateTime Today = new(2026, 10, 11);

    [Theory]
    [InlineData("8001015009087")]
    [InlineData("0002290123088")]
    [InlineData("9506150123187")]
    [InlineData("2610115009080")]
    [InlineData("9912315009081")]
    public void Validate_ValidIdNumber_ReturnsOk(string id)
    {
        ValidationOutcome result = IdNumberValidator.Validate(id, Today);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Validate_IdNumberWithSpacesAroundIt_IsTrimmedAndAccepted()
    {
        Assert.True(IdNumberValidator.Validate(" 8001015009087 ", Today).IsValid);
    }

    [Theory]
    [InlineData("8001015009088")]
    [InlineData("0002290123089")]
    [InlineData("9506150123188")]
    [InlineData("2610115009081")]
    [InlineData("9912315009082")]
    public void Validate_WrongCheckDigit_IsRejected(string id)
    {
        ValidationOutcome result = IdNumberValidator.Validate(id, Today);

        Assert.False(result.IsValid);
        Assert.Contains("not valid", result.ErrorMessage);
    }

    [Theory]
    // month 13, day 32, 31 April, 30 February, 29 February in a non-leap year, day 00, month 00
    [InlineData("9013015009081")]
    [InlineData("9001325009081")]
    [InlineData("9004315009087")]
    [InlineData("9002305009083")]
    [InlineData("0102295009082")]
    [InlineData("9001005009088")]
    [InlineData("9000015009088")]
    public void Validate_ImpossibleDateOfBirth_IsRejected(string id)
    {
        ValidationOutcome result = IdNumberValidator.Validate(id, Today);

        Assert.False(result.IsValid);
        Assert.Contains("date of birth", result.ErrorMessage);
    }

    [Theory]
    // 12 digits, 14 digits, letters, a letter O instead of a zero, a hyphen, spaces inside
    [InlineData("800101500908")]
    [InlineData("80010150090870")]
    [InlineData("abcdefghijklm")]
    [InlineData("8001015009O87")]
    [InlineData("800101-5009087")]
    [InlineData("800101 5009 087")]
    public void Validate_NotExactly13Digits_IsRejected(string id)
    {
        ValidationOutcome result = IdNumberValidator.Validate(id, Today);

        Assert.False(result.IsValid);
        Assert.Contains("13 digits", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyInput_IsRejected(string? id)
    {
        ValidationOutcome result = IdNumberValidator.Validate(id, Today);

        Assert.False(result.IsValid);
        Assert.Contains("Enter your", result.ErrorMessage);
    }

    [Fact]
    public void TryGetDateOfBirth_YearInThe1900s_ReturnsCorrectDate()
    {
        bool ok = IdNumberValidator.TryGetDateOfBirth("8001015009087", Today, out DateTime dob);

        Assert.True(ok);
        Assert.Equal(new DateTime(1980, 1, 1), dob);
    }

    [Fact]
    public void TryGetDateOfBirth_LeapDayIn2000_IsAccepted()
    {
        bool ok = IdNumberValidator.TryGetDateOfBirth("0002290123088", Today, out DateTime dob);

        Assert.True(ok);
        Assert.Equal(new DateTime(2000, 2, 29), dob);
    }

    [Fact]
    public void TryGetDateOfBirth_BornToday_IsAccepted()
    {
        bool ok = IdNumberValidator.TryGetDateOfBirth("2610115009080", Today, out DateTime dob);

        Assert.True(ok);
        Assert.Equal(Today, dob);
    }

    [Fact]
    public void TryGetDateOfBirth_TwoDigitYearAfterThisYear_IsTreatedAsThe1900s()
    {
        // 27 would be 2027, which is in the future, so it must mean 1927.
        bool ok = IdNumberValidator.TryGetDateOfBirth("2701015009087", Today, out DateTime dob);

        Assert.True(ok);
        Assert.Equal(new DateTime(1927, 1, 1), dob);
    }

    [Fact]
    public void IsValid_ReturnsSameAnswerAsValidate()
    {
        Assert.True(IdNumberValidator.IsValid("9506150123187", Today));
        Assert.False(IdNumberValidator.IsValid("9506150123188", Today));
    }
}
