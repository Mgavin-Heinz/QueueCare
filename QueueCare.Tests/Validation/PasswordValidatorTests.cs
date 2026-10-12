using System;
using QueueCare.Validation;
using Xunit;

namespace QueueCare.Tests.Validation;

// Requirement FR-08 (test case TC-008 in the test workbook).
public class PasswordValidatorTests
{
    [Theory]
    [InlineData("Passw0rdOK")]
    [InlineData("Abcdefg1")]
    [InlineData("Queue2026Care")]
    public void Validate_StrongPassword_ReturnsOk(string password)
    {
        ValidationOutcome result = PasswordValidator.Validate(password);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    // abc123 (too short), alllowercase1 (no uppercase), ALLUPPER1 (no lowercase),
    // NoDigitsHere (no digit), Sh0rtA1 (7 characters), empty
    // The second value is the first rule that must be named in the message.
    [InlineData("abc123", "at least 8 characters")]
    [InlineData("alllowercase1", "an uppercase letter")]
    [InlineData("ALLUPPER1", "a lowercase letter")]
    [InlineData("NoDigitsHere", "a digit")]
    [InlineData("Sh0rtA1", "at least 8 characters")]
    [InlineData("", "at least 8 characters")]
    public void Validate_WeakPassword_IsRejectedAndNamesTheMissingRule(string password, string expectedRule)
    {
        ValidationOutcome result = PasswordValidator.Validate(password);

        Assert.False(result.IsValid);
        Assert.Contains(expectedRule, result.ErrorMessage);
    }

    [Fact]
    public void Validate_Null_IsRejected()
    {
        Assert.False(PasswordValidator.Validate(null).IsValid);
    }

    [Fact]
    public void Validate_EightCharactersWithAllThreeKinds_IsTheShortestAcceptedPassword()
    {
        Assert.True(PasswordValidator.Validate("Abcdefg1").IsValid);
        Assert.False(PasswordValidator.Validate("Abcdef1").IsValid);
    }

    [Fact]
    public void Validate_NoSpecialCharacterNeeded()
    {
        // The SRS does not require symbols, so a password without one must be accepted.
        Assert.True(PasswordValidator.Validate("Passw0rdOK").IsValid);
    }
}
