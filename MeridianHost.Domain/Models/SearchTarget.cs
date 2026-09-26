using CSharpFunctionalExtensions;
using MeridianHost.Domain.Enums;

using static MeridianHost.Domain.Models.Extensions.CreateTarget;

namespace MeridianHost.Domain.Models;

public class SearchTarget
{
    public Guid Id { get; private set; }
    public string Value { get; private set; }
    public SearchTargetType TargetType { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }

    private SearchTarget(Guid id, string value, SearchTargetType targetType, DateTimeOffset startedAt, DateTimeOffset? finishedAt)
    {
        Id = id;
        Value = value;
        TargetType = targetType;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }

    public static Result<SearchTarget> Create(string value, SearchTargetType type)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return Result.Failure<SearchTarget>($"Error: {validationResult.Error}");

        return type switch
        {
            SearchTargetType.Email => CreateEmail(value),
            SearchTargetType.Username => CreateUsername(value),
            SearchTargetType.IpAddress => CreateIpAddress(value),
            SearchTargetType.Domain => CreateDomainName(value),
            _ => Result.Failure<SearchTarget>("Unsupported search target type.")
        };
    }

    internal static SearchTarget CreateValidated(string value, SearchTargetType type) =>
        new(Guid.NewGuid(), value, type, DateTimeOffset.UtcNow, null);

    public static Result Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure("invalid value");

        if (value.Length is < 3 or > 256)
            return Result.Failure("invalid value length");

        return Result.Success();
    }
}
