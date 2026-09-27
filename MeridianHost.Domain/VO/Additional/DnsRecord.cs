using CSharpFunctionalExtensions;
using MeridianHost.Domain.Enums.Records;

namespace MeridianHost.Domain.VO.Additional;

public readonly struct DnsRecord
{
    public DnsRecordType Type { get; }
    public string HostName { get; }
    public string Value { get; }
    public int TTL { get; }

    private DnsRecord(DnsRecordType type, string hostName, string value, int ttl)
    {
        Type = type;
        HostName = hostName;
        Value = value;
        TTL = ttl;
    }

    public static Result<DnsRecord> Create(DnsRecordType type, string hostName, string value, int ttl)
    {
        var validationResult = Validate(hostName, value, ttl);
        if (validationResult.IsFailure)
            return Result.Failure<DnsRecord>($"Error: {validationResult.Error}");

        return Result.Success(new DnsRecord(type, hostName, value, ttl));
    }

    public static Result Validate(string hostName, string value, int ttl)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return Result.Failure("invalid hostname");
        
        if (hostName.Length is < 3 or > 512)
            return Result.Failure("host name must be between 3 and 512 characters");
        
        if (string.IsNullOrWhiteSpace(value))
            return Result.Failure("invalid value");
        
        if (value.Length is > 2048)
            return Result.Failure("value must not be greater than 2048 characters");
        
        if (ttl is < 60 or > 32768)
            return Result.Failure("invalid TTL value");

        return Result.Success();
    }
}