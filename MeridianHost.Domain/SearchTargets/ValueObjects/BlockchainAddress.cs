using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;

namespace MeridianHost.Domain.SearchTargets.ValueObjects;

public readonly partial struct BlockchainAddress : IEquatable<BlockchainAddress>
{
    public string Value { get; }
    public string Network { get; }

    private BlockchainAddress(string value, string network)
    {
        Value = value;
        Network = network;
    }

    public static Result<BlockchainAddress> Create(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return Result.Failure<BlockchainAddress>("Blockchain address cannot be empty.");

        var normalized = rawValue.Trim();
        var network = DetectNetwork(normalized);

        if (network is null)
            return Result.Failure<BlockchainAddress>("Unsupported or invalid blockchain address format.");

        return Result.Success(new BlockchainAddress(normalized, network));
    }

    public static BlockchainAddress Load(string value, string network = "Unknown")
        => new BlockchainAddress(value, network);

    public static bool TryParse(string value, out BlockchainAddress address)
    {
        var result = Create(value);
        if (result.IsSuccess)
        {
            address = result.Value;
            return true;
        }

        address = default;
        return false;
    }

    public static string? DetectNetwork(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        if (EvmRegex().IsMatch(address))
            return "Ethereum/EVM";

        if (BtcSegwitRegex().IsMatch(address) || BtcLegacyRegex().IsMatch(address))
            return "Bitcoin";

        if (TronRegex().IsMatch(address))
            return "Tron";

        if (SolanaRegex().IsMatch(address))
            return "Solana";

        return null;
    }

    public bool Equals(BlockchainAddress other)
        => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj)
        => obj is BlockchainAddress other && Equals(other);

    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public static bool operator ==(BlockchainAddress left, BlockchainAddress right)
        => left.Equals(right);

    public static bool operator !=(BlockchainAddress left, BlockchainAddress right)
        => !left.Equals(right);

    public override string ToString()
        => Value;

    [GeneratedRegex(@"^0x[a-fA-F0-9]{40}$", RegexOptions.Compiled)]
    private static partial Regex EvmRegex();

    [GeneratedRegex(@"^(bc1|[13])[a-zA-HJ-NP-Z0-9]{25,62}$", RegexOptions.Compiled)]
    private static partial Regex BtcLegacyRegex();

    [GeneratedRegex(@"^bc1[qpzry9x8gf2tvdw0s3jn54khce6mua7l]{38,59}$", RegexOptions.Compiled)]
    private static partial Regex BtcSegwitRegex();

    [GeneratedRegex(@"^T[1-9A-HJ-NP-Za-km-z]{33}$", RegexOptions.Compiled)]
    private static partial Regex TronRegex();

    [GeneratedRegex(@"^[1-9A-HJ-NP-Za-km-z]{32,44}$", RegexOptions.Compiled)]
    private static partial Regex SolanaRegex();
}
