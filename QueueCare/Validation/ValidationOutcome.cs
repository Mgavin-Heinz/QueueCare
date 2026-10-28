namespace QueueCare.Validation;

/// <summary>
/// The result of checking one value against a QueueCare rule.
/// (Named ValidationOutcome so it does not clash with
/// System.ComponentModel.DataAnnotations.ValidationResult.)
/// </summary>
public sealed record ValidationOutcome(bool IsValid, string? ErrorMessage)
{
    public static ValidationOutcome Ok { get; } = new(true, null);

    public static ValidationOutcome Fail(string message) => new(false, message);
}
