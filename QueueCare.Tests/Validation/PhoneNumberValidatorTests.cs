using System;
using QueueCare.Validation;
using Xunit;

namespace QueueCare.Tests.Validation;

// Requirement FR-03 (test case TC-003 in the test workbook).
// Normalize is the form used when checking for duplicate numbers (FR-04).
public class PhoneNumberValidatorTests
{
    [Theory]
    [InlineData("0821234567", "0821234567")]
    [InlineData("+27821234567", "0821234567")]
    [InlineData("082 123 4567", "0821234567")]
    [InlineData("082-123-4567", "0821234567")]
    [InlineData("+27 82 123 4567", "0821234567")]
    [InlineData("0111234567", "0111234567")]
    public void Normalize_ValidNumber_ReturnsTenDigitsStartingWithZero(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberValidator.Normalize(input));
        Assert.True(PhoneNumberValidator.Validate(input).IsValid);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("abcdefghij")]
    [InlineData("+1 555 0100")]
    [InlineData("082123456")]
    [InlineData("08212345678")]
    [InlineData("+2782123456")]
    [InlineData("+278212345678")]
    [InlineData("0821234567x")]
    [InlineData("8212345678")]
    [InlineData("(082) 123 4567")]
    public void Validate_InvalidNumber_IsRejected(string input)
    {
        ValidationOutcome result = PhoneNumberValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains("South African cellphone number", result.ErrorMessage);
        Assert.Null(PhoneNumberValidator.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyInput_IsRejected(string? input)
    {
        ValidationOutcome result = PhoneNumberValidator.Validate(input);

        Assert.False(result.IsValid);
        Assert.Contains("Enter your cellphone number", result.ErrorMessage);
    }

    [Fact]
    public void Normalize_PlusTwentySevenAndZeroForm_GiveTheSameNumber()
    {
        // So the duplicate check (FR-04) treats them as one number.
        Assert.Equal(
            PhoneNumberValidator.Normalize("0821234567"),
            PhoneNumberValidator.Normalize("+27821234567"));
    }
}
