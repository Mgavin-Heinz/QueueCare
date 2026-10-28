using System;
namespace QueueCare.Validation;

/// <summary>
/// Validates a South African ID number (requirement FR-02).
/// Rules: exactly 13 digits, the first six digits (YYMMDD) are a real date of birth,
/// and the last digit is a correct check digit (Luhn algorithm).
/// </summary>
public static class IdNumberValidator
{
    public const int Length = 13;

    /// <param name="idNumber">The text entered by the user.</param>
    /// <param name="today">Only for testing. Defaults to today's date.</param>
    public static ValidationOutcome Validate(string? idNumber, DateTime? today = null)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return ValidationOutcome.Fail("Enter your 13-digit SA ID number.");
        }

        string id = idNumber.Trim();

        if (id.Length != Length || !Digits.AllDigits(id))
        {
            return ValidationOutcome.Fail("An SA ID number must be exactly 13 digits (numbers only).");
        }

        if (!TryGetDateOfBirth(id, today ?? DateTime.Today, out _))
        {
            return ValidationOutcome.Fail("The first six digits of this ID number are not a valid date of birth (YYMMDD).");
        }

        if (!HasValidCheckDigit(id))
        {
            return ValidationOutcome.Fail("This ID number is not valid. Please check the digits and try again.");
        }

        return ValidationOutcome.Ok;
    }

    public static bool IsValid(string? idNumber, DateTime? today = null) =>
        Validate(idNumber, today).IsValid;

    /// <summary>
    /// Reads the date of birth from the first six digits. The century is chosen so the
    /// date is not in the future: 2000 + YY if that is today or earlier, otherwise 1900 + YY.
    /// </summary>
    public static bool TryGetDateOfBirth(string id, DateTime today, out DateTime dateOfBirth)
    {
        dateOfBirth = default;

        if (id.Length < 6 || !Digits.AllDigits(id.Substring(0, 6)))
        {
            return false;
        }

        int yy = int.Parse(id.Substring(0, 2));
        int month = int.Parse(id.Substring(2, 2));
        int day = int.Parse(id.Substring(4, 2));

        foreach (int year in new[] { 2000 + yy, 1900 + yy })
        {
            if (IsRealDate(year, month, day))
            {
                var candidate = new DateTime(year, month, day);
                if (candidate <= today.Date)
                {
                    dateOfBirth = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsRealDate(int year, int month, int day) =>
        month >= 1 && month <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month);

    /// <summary>
    /// Luhn check. Counting from the left (0-based), the digits at odd positions are doubled
    /// (subtract 9 if the result is above 9) and the total must be a multiple of 10.
    /// </summary>
    private static bool HasValidCheckDigit(string id)
    {
        int sum = 0;

        for (int i = 0; i < id.Length; i++)
        {
            int digit = id[i] - '0';

            if (i % 2 == 1)
            {
                digit *= 2;
                if (digit > 9)
                {
                    digit -= 9;
                }
            }

            sum += digit;
        }

        return sum % 10 == 0;
    }
}
