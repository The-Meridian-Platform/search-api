using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace MeridianHost.Domain.SearchTargets.ValueObjects;

public readonly partial struct PhoneNumber : IEquatable<PhoneNumber>
{
    public string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static Result<PhoneNumber> Create(string rawValue)
    {
        var normalized = Normalize(rawValue);
        var validation = Validate(normalized);
        if (validation.IsFailure)
            return Result.Failure<PhoneNumber>(validation.Error);

        return Result.Success(new PhoneNumber(normalized));
    }

    public static PhoneNumber Load(string value)
        => new PhoneNumber(value);

    public static string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var trimmed = input.Trim();
        bool hasPlus = trimmed.StartsWith('+');
        var digitsOnly = DigitsRegex().Replace(trimmed, string.Empty);

        return hasPlus ? $"+{digitsOnly}" : digitsOnly;
    }

    private static Result Validate(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized))
            return Result.Failure("Phone number cannot be empty.");

        var digits = normalized.TrimStart('+');
        if (digits.Length < 7 || digits.Length > 16)
            return Result.Failure("Phone number length must be between 7 and 16 digits.");

        if (!digits.All(char.IsDigit))
            return Result.Failure("Phone number must contain only numeric digits.");

        return Result.Success();
    }

    public bool Equals(PhoneNumber other)
        => string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj)
        => obj is PhoneNumber other && Equals(other);

    public override int GetHashCode()
        => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(PhoneNumber left, PhoneNumber right)
        => left.Equals(right);

    public static bool operator !=(PhoneNumber left, PhoneNumber right)
        => !left.Equals(right);

    public override string ToString()
        => Value;

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex DigitsRegex();
}
