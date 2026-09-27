using CSharpFunctionalExtensions;
using MeridianHost.Domain.VO.Additional;

namespace MeridianHost.Domain.VO;

public readonly struct Username : IEquatable<Username>
{
    public string Value { get; }

    private Username(string value) => Value = value;

    public static Result<Username> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<Username>("Username is required.");

        var normalizedValue = value.Trim();

        var validationResult = Validate(normalizedValue);
        if (validationResult.IsFailure)
            return Result.Failure<Username>(validationResult.Error);

        return Result.Success(new Username(normalizedValue));
    }

    public static Username Load(string value)
        => new Username(value);

    private static Result Validate(string value)
    {
        if (value.Length is < 3 or > 64)
            return Result.Failure("Username must be between 3 and 64 characters.");

        if (value.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '_' and not '-' and not '.'))
        {
            return Result.Failure(
                "Username can contain only letters, digits, '.', '_' and '-'.");
        }

        return Result.Success();
    }
    
    public static bool TryParse(string? value, out Username? userName)
    {
        userName = default;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;
        
        value = value.Trim();
        
        if (value.Length is < 3 or > 64)
            return false;

        if (value.Any(character =>
                !char.IsLetterOrDigit(character) && character is not '_' and not '-' and not '.'))
            return false;

        userName = new Username(value);

        return true;
    }

    public bool Equals(Username other) =>
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is Username other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(Username left, Username right) => left.Equals(right);

    public static bool operator !=(Username left, Username right) => !left.Equals(right);
}
