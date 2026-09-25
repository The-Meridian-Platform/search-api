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

    private static Result Validate(string value)
    {
        if (value.Length is < 3 or > 128) 
            return Result.Failure("length must be greater than 3 and less than 128");
        
        if (!value.Contains('@') || (value[0] == '@' || value[value.Length - 1] == '@'))
            return Result.Failure("invalid email address format");
        
        return Result.Success();
    }
}