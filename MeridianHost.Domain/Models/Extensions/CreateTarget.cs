using System.Net;
using CSharpFunctionalExtensions;
using MeridianHost.Domain.Enums;
using MeridianHost.Domain.VO;

namespace MeridianHost.Domain.Models.Extensions;

public static class CreateTarget
{
    public static Result<SearchTarget> CreateEmail(string value)
    {
        var email = Email.Create(value);

        return email.IsFailure
            ? Result.Failure<SearchTarget>(email.Error)
            : Result.Success(SearchTarget.CreateValidated(
                email.Value.Value,
                SearchTargetType.Email));
    }

    public static Result<SearchTarget> CreateDomainName(string value)
    {
        var domainName = DomainName.Create(value);

        return domainName.IsFailure
            ? Result.Failure<SearchTarget>(domainName.Error)
            : Result.Success(SearchTarget.CreateValidated(
                domainName.Value.Value,
                SearchTargetType.Domain));
    }

    public static Result<SearchTarget> CreateIpAddress(string value)
    {
        if (!IPAddress.TryParse(value, out var address))
            return Result.Failure<SearchTarget>("Invalid IP address.");

        return Result.Success(SearchTarget.CreateValidated(
            address.ToString(),
            SearchTargetType.IpAddress));
    }

    public static Result<SearchTarget> CreateUsername(string value)
    {
        var username = Username.Create(value);

        return username.IsFailure
            ? Result.Failure<SearchTarget>(username.Error)
            : Result.Success(SearchTarget.CreateValidated(
                username.Value.Value,
                SearchTargetType.Username));
    }
}
