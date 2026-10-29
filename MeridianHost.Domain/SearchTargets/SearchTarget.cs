using System.Net;
using CSharpFunctionalExtensions;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Domain.SearchTargets.ValueObjects;
using static MeridianHost.Domain.SearchTargets.Extensions.CreateTarget;

namespace MeridianHost.Domain.SearchTargets;

public sealed class SearchTarget
{
    public Guid Id { get; private set; }
    public string Value { get; private set; }
    public SearchTargetType TargetType { get; private set; }

    private SearchTarget(Guid id, string value, SearchTargetType targetType)
    {
        Id = id;
        Value = value;
        TargetType = targetType;
    }

    public static Result<SearchTarget> Create(string value, SearchTargetType? targetType)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return Result.Failure<SearchTarget>($"Error: {validationResult.Error}");

        return targetType switch
        {
            SearchTargetType.IpAddress => CreateIpAddress(value),
            SearchTargetType.Domain => CreateDomainName(value),
            SearchTargetType.Username => CreateUsername(value),
            SearchTargetType.Email => CreateEmail(value),
            null => DetectType(value),
            _ => Result.Failure<SearchTarget>("invalid target type")
        };
    }

    private static Result<SearchTarget> DetectType(string value)
        => value switch
        {
            _ when IPAddress.TryParse(value, out _) => CreateIpAddress(value),
            _ when Email.TryParse(value, out _) => CreateEmail(value),
            _ when DomainName.TryParse(value, out _) => CreateDomainName(value),
            _ when Username.TryParse(value, out _) => CreateUsername(value),
            _ => Result.Failure<SearchTarget>("invalid value")
        };

    public static SearchTarget Load(Guid id, string value, SearchTargetType targetType)
        => new SearchTarget(id, value, targetType);

    internal static SearchTarget CreateValidated(string value, SearchTargetType type) =>
        new(Guid.NewGuid(), value, type);

    public static Result Validate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure("invalid value");

        if (value.Length is < 3 or > 256)
            return Result.Failure("invalid value length");

        return Result.Success();
    }
}
