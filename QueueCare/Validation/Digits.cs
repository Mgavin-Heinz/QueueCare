namespace QueueCare.Validation;

internal static class Digits
{
    /// <summary>True when the text is not empty and every character is 0 to 9.</summary>
    public static bool AllDigits(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        foreach (char c in text)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        return true;
    }
}
