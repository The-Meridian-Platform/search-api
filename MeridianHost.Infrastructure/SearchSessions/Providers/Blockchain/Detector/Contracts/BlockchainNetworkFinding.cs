namespace MeridianHost.Infrastructure.SearchSessions.Providers.Blockchain.Detector.Contracts;

public sealed class BlockchainNetworkFinding
{
    public string Address { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string AddressFormat { get; set; } = string.Empty;
    public string ExplorerUrl { get; set; } = string.Empty;
    public List<string> SupportedChains { get; set; } = [];
    public string Description { get; set; } = string.Empty;
}
