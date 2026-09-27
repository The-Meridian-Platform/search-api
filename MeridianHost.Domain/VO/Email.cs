using CSharpFunctionalExtensions;

namespace MeridianHost.Domain.VO;

public readonly struct Email
{
    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Result<Email> Create(string value)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return Result.Failure<Email>($"Error: {validationResult.Error}");

        return Result.Success(new Email(value));
    }

    public static Email Load(string value)
        => new Email(value);

    private static Result Validate(string value)
    {
        if (value.Length is < 3 or > 128) 
            return Result.Failure("length must be greater than 3 and less than 128");
        
        if (!value.Contains('@') || (value[0] == '@' || value[value.Length - 1] == '@'))
            return Result.Failure("invalid email address format");
        
        if (value.Count(c => c == '@') != 1)
            return Result.Failure("invalid email address");
        
        return Result.Success();
    }

    public static bool TryParse(string? value, out Email? email)
    {
        email = default;
        
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();

        if (value.Length is < 3 or > 128)
            return false;

        var atIndex = value.IndexOf('@');

        if (atIndex <= 0 || atIndex == value.Length - 1)
            return false;

        email = new Email(value);
        return true;
    }
}