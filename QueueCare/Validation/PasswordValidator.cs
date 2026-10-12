using System;
using System.Collections.Generic;
using System.Linq;
namespace QueueCare.Validation;

/// <summary>
/// Checks the password rules (requirement FR-08): at least 8 characters with an
/// uppercase letter, a lowercase letter and a digit.
/// </summary>
public static class PasswordValidator
{
    public const int MinLength = 8;

    public static ValidationOutcome Validate(string? password)
    {
        string text = password ?? string.Empty;
        var problems = new List<string>();

        if (text.Length < MinLength)
        {
            problems.Add($"be at least {MinLength} characters long");
        }

        if (!text.Any(char.IsUpper))
        {
            problems.Add("contain an uppercase letter");
        }

        if (!text.Any(char.IsLower))
        {
            problems.Add("contain a lowercase letter");
        }

        if (!text.Any(c => c >= '0' && c <= '9'))
        {
            problems.Add("contain a digit");
        }

        return problems.Count == 0
            ? ValidationOutcome.Ok
            : ValidationOutcome.Fail("Password must " + string.Join(", ", problems) + ".");
    }

    public static bool IsValid(string? password) => Validate(password).IsValid;
}
