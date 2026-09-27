using System.Net;
using CSharpFunctionalExtensions;
using MeridianHost.Domain.Enums;
using MeridianHost.Domain.VO;
using static MeridianHost.Domain.Models.Extensions.CreateTarget;

namespace MeridianHost.Domain.Models;

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

    public static Result<SearchTarget> Create(string value)
    {
        var validationResult = Validate(value);
        if (validationResult.IsFailure)
            return Result.Failure<SearchTarget>($"Error: {validationResult.Error}");

        return value switch
        {
            _ when IPAddress.TryParse(value, out var ip) => CreateIpAddress(value),
            _ when DomainName.TryParse(value, out var domainName) => CreateDomainName(value),
            _ when Username.TryParse(value, out var userName) => CreateUsername(value),
            _ when Email.TryParse(value, out var email) => CreateEmail(value),
            _ => Result.Failure<SearchTarget>("invalid value")
        };
    }

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
