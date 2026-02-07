using System.ComponentModel.DataAnnotations;

namespace UrbanX.Shared.Security;

/// <summary>
/// Input validation utilities for production-ready API endpoints
/// </summary>
public static class RequestValidation
{
    /// <summary>
    /// Validates an object using Data Annotations
    /// </summary>
    public static void Validate<T>(T obj) where T : class
    {
        var context = new ValidationContext(obj);
        var results = new List<ValidationResult>();
        
        if (!Validator.TryValidateObject(obj, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Validation failed: {errors}");
        }
    }

    /// <summary>
    /// Validates a Guid parameter
    /// </summary>
    public static void ValidateGuid(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ValidationException($"{parameterName} cannot be empty.");
        }
    }

    /// <summary>
    /// Validates a required string parameter
    /// </summary>
    public static void ValidateRequiredString(string? value, string parameterName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException($"{parameterName} is required.");
        }

        if (maxLength.HasValue && value.Length > maxLength.Value)
        {
            throw new ValidationException($"{parameterName} cannot exceed {maxLength.Value} characters.");
        }
    }

    /// <summary>
    /// Validates a numeric value is within range
    /// </summary>
    public static void ValidateRange(decimal value, string parameterName, decimal min, decimal max)
    {
        if (value < min || value > max)
        {
            throw new ValidationException($"{parameterName} must be between {min} and {max}.");
        }
    }

    /// <summary>
    /// Validates a positive number
    /// </summary>
    public static void ValidatePositive(decimal value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ValidationException($"{parameterName} must be greater than zero.");
        }
    }

    /// <summary>
    /// Validates an email address format
    /// </summary>
    public static void ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ValidationException("Email is required.");
        }

        var emailAttribute = new EmailAddressAttribute();
        if (!emailAttribute.IsValid(email))
        {
            throw new ValidationException("Invalid email format.");
        }
    }
}
