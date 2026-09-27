using System.Globalization;
using CSharpFunctionalExtensions;
using MeridianHost.Domain.VO.Additional;

namespace MeridianHost.Domain.VO;

public readonly struct DomainName : IEquatable<DomainName>
{
    public string Value { get; }
    public IReadOnlyList<DnsRecord>? DnsRecords { get; }

    private DomainName(string value, IReadOnlyList<DnsRecord>? dnsRecords)
    {
        Value = value;
        DnsRecords = dnsRecords;
    }

    public static Result<DomainName> Create(
        string? value,
        IEnumerable<DnsRecord>? records = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure<DomainName>("Domain name is required.");

        var input = value.Trim().TrimEnd('.');

        string normalizedValue;

        try
        {
            normalizedValue = new IdnMapping().GetAscii(input).ToLowerInvariant();
        }
        catch (ArgumentException)
        {
            return Result.Failure<DomainName>("Invalid domain name.");
        }

        var validationResult = Validate(normalizedValue);
        if (validationResult.IsFailure)
            return Result.Failure<DomainName>(validationResult.Error);

        return Result.Success(new DomainName(
            normalizedValue,
            records?.ToArray() ?? []));
    }

    private static Result Validate(string value)
    {
        if (value.Length is < 3 or > 253)
            return Result.Failure("Domain name must be between 3 and 253 characters.");

        var labels = value.Split('.');

        if (labels.Any(label =>
                label.Length is < 1 or > 63 ||
                label[0] == '-' ||  
                label[^1] == '-' ||
                label.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            return Result.Failure("Invalid domain name.");
        }

        return Result.Success();
    }
    
    public static bool TryParse(string? value, out DomainName? domainName)
    {
        domainName = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();
        
        if (value.Length is < 3 or > 253)
            return false;

        var labels = value.Split('.');

        if (labels.Any(label =>
                label.Length is < 1 or > 63 ||
                label[0] == '-' ||
                label[^1] == '-' ||
                label.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-')))
        {
            return false;
        }

        domainName = new DomainName(value, null);

        return true;
    }

    public bool Equals(DomainName other) =>
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) =>
        obj is DomainName other && Equals(other);

    public override int GetHashCode() =>
        StringComparer.Ordinal.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(DomainName left, DomainName right) => left.Equals(right);

    public static bool operator !=(DomainName left, DomainName right) => !left.Equals(right);
}
