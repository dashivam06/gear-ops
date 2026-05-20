using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace gearOps.Application.Helpers;

/// <summary>
/// Validates password strength against configurable security requirements.
/// </summary>
public static class PasswordValidator
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /// <summary>
    /// Validates that a password meets security requirements.
    /// Returns a list of violation messages (empty = valid).
    /// </summary>
    public static List<string> Validate(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required.");
            return errors;
        }

        if (password.Length < MinLength)
            errors.Add($"Password must be at least {MinLength} characters long.");

        if (password.Length > MaxLength)
            errors.Add($"Password must not exceed {MaxLength} characters.");

        if (!Regex.IsMatch(password, @"[A-Z]"))
            errors.Add("Password must contain at least one uppercase letter.");

        if (!Regex.IsMatch(password, @"[a-z]"))
            errors.Add("Password must contain at least one lowercase letter.");

        if (!Regex.IsMatch(password, @"[0-9]"))
            errors.Add("Password must contain at least one digit.");

        if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]"))
            errors.Add("Password must contain at least one special character (!@#$%^&*...).");

        return errors;
    }

    /// <summary>
    /// Validates a password and throws InvalidPasswordException if it fails.
    /// </summary>
    public static void ValidateOrThrow(string password)
    {
        var errors = Validate(password);
        if (errors.Count > 0)
        {
            throw new Exceptions.InvalidPasswordException(string.Join(" ", errors));
        }
    }
}
