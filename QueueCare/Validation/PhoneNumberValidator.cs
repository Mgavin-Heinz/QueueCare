using System;
using System.Text;

namespace QueueCare.Validation;

/// <summary>
/// Validates a South African cellphone number (requirement FR-03):
/// 10 digits starting with 0, or +27 followed by 9 digits.
/// Spaces and hyphens are allowed as separators and ignored.
/// </summary>
public static class PhoneNumberValidator
{
    public static ValidationOutcome Validate(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return ValidationOutcome.Fail("Enter your cellphone number.");
        }

        if (Normalize(phoneNumber) is null)
        {
            return ValidationOutcome.Fail(
                "Enter a South African cellphone number: 10 digits starting with 0 (for example 0821234567) or +27 followed by 9 digits.");
        }

        return ValidationOutcome.Ok;
    }

    public static bool IsValid(string? phoneNumber) => Validate(phoneNumber).IsValid;

    /// <summary>
    /// Returns the number in one standard form (10 digits starting with 0), or null if it
    /// is not valid. Use this form when saving and when checking for duplicates (FR-04),
    /// so that +27821234567 and 0821234567 count as the same number.
    /// </summary>
    public static string? Normalize(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var cleaned = new StringBuilder();
        foreach (char c in phoneNumber.Trim())
        {
            if (c != ' ' && c != '-')
            {
                cleaned.Append(c);
            }
        }

        string text = cleaned.ToString();

        if (text.StartsWith("+27", StringComparison.Ordinal))
        {
            string rest = text.Substring(3);
            return rest.Length == 9 && Digits.AllDigits(rest) ? "0" + rest : null;
        }

        return text.Length == 10 && text[0] == '0' && Digits.AllDigits(text) ? text : null;
    }
}
