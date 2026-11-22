using System.Runtime.CompilerServices;
using System.Text.Json;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;
using MeridianHost.Domain.SearchTargets.Enum;
using MeridianHost.Domain.SearchTargets.ValueObjects;
using MeridianHost.Infrastructure.SearchSessions.Providers.Blockchain.Detector.Contracts;

namespace MeridianHost.Infrastructure.SearchSessions.Providers.Blockchain.Detector;

public class BlockchainNetworkDetectorProvider : ISearchProvider
{
    public string Key { get; } = "blockchain-network-detector";

    public bool CanHandle(SearchTarget target)
        => target.TargetType == SearchTargetType.BlockchainAddress;

    public async IAsyncEnumerable<RawSearchFinding> ReadAllAsync(
        SearchTarget target,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var address = target.Value.Trim();
        var network = BlockchainAddress.DetectNetwork(address);

        if (network is null)
            yield break;

        var finding = BuildFinding(address, network);
        yield return new RawSearchFinding("blockchain_network_record", JsonSerializer.Serialize(finding));

        await Task.CompletedTask;
    }

    private static BlockchainNetworkFinding BuildFinding(string address, string network)
    {
        return network switch
        {
            "Ethereum/EVM" => new BlockchainNetworkFinding
            {
                Address = address,
                Network = "Ethereum / EVM Compatible",
                AddressFormat = "Hexadecimal 20-byte (ERC-55 / EIP-1191 Checksum)",
                ExplorerUrl = $"https://etherscan.io/address/{address}",
                SupportedChains =
                [
                    "Ethereum (Mainnet)",
                    "Arbitrum One",
                    "Optimism (OP Mainnet)",
                    "Base (Coinbase L2)",
                    "BNB Smart Chain (BSC)",
                    "Polygon (PoS)",
                    "Avalanche (C-Chain)"
                ],
                Description = "Address is compatible with all EVM-based networks. May hold balances and transaction history across multiple L1 and L2 chains simultaneously."
            },
            "Bitcoin" => new BlockchainNetworkFinding
            {
                Address = address,
                Network = "Bitcoin (BTC)",
                AddressFormat = DetectBtcFormat(address),
                ExplorerUrl = $"https://blockstream.info/address/{address}",
                SupportedChains = ["Bitcoin Mainnet", "Bitcoin Lightning (On-chain channel funding)"],
                Description = "Native Bitcoin network address. Tracked using the UTXO transaction model."
            },
            "Tron" => new BlockchainNetworkFinding
            {
                Address = address,
                Network = "Tron (TRX)",
                AddressFormat = "Base58Check (21-byte with 0x41 prefix)",
                ExplorerUrl = $"https://tronscan.org/#/address/{address}",
                SupportedChains = ["Tron Mainnet (TRC-20 / USDT hub)"],
                Description = "Address on the Tron network. Primary hub for USDT transactions under the TRC-20 token standard."
            },
            "Solana" => new BlockchainNetworkFinding
            {
                Address = address,
                Network = "Solana (SOL)",
                AddressFormat = "Base58 (32-byte Ed25519 public key)",
                ExplorerUrl = $"https://solscan.io/account/{address}",
                SupportedChains = ["Solana Mainnet-Beta"],
                Description = "Account address on the high-throughput Solana network (SPL tokens and programs)."
            },
            _ => new BlockchainNetworkFinding
            {
                Address = address,
                Network = network,
                AddressFormat = "Unknown Format",
                ExplorerUrl = string.Empty,
                SupportedChains = [network],
                Description = "Identified blockchain network."
            }
        };
    }

    private static string DetectBtcFormat(string address)
    {
        if (address.StartsWith("bc1q", StringComparison.OrdinalIgnoreCase))
            return "Native SegWit (Bech32 - P2WPKH / P2WSH)";

        if (address.StartsWith("bc1p", StringComparison.OrdinalIgnoreCase))
            return "Taproot (Bech32m - P2TR)";

        if (address.StartsWith('1'))
            return "Legacy (Base58 - P2PKH)";

        if (address.StartsWith('3'))
            return "Nested SegWit / Script Hash (Base58 - P2SH)";

        return "Bitcoin Standard Address";
    }
}
