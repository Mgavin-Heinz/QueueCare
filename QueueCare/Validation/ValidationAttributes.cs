using System;
using System.ComponentModel.DataAnnotations;

namespace QueueCare.Validation;

// Use these on the registration view model so the form shows the same messages as the tests.
// Empty values are allowed here: use [Required] when a field must be filled in.
// (A patient may register with an SA ID number OR a cellphone number, so each one is optional.)

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SaIdNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        ValidationOutcome outcome = IdNumberValidator.Validate(text);
        return outcome.IsValid ? ValidationResult.Success : new ValidationResult(outcome.ErrorMessage);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SaPhoneNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        ValidationOutcome outcome = PhoneNumberValidator.Validate(text);
        return outcome.IsValid ? ValidationResult.Success : new ValidationResult(outcome.ErrorMessage);
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string text || text.Length == 0)
        {
            return ValidationResult.Success;
        }

        ValidationOutcome outcome = PasswordValidator.Validate(text);
        return outcome.IsValid ? ValidationResult.Success : new ValidationResult(outcome.ErrorMessage);
    }
}
