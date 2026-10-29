using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Domain.SearchTargets;

namespace MeridianHost.Application.SearchSessions.Execute.Providers;

/// <summary>
/// A provider that "discards" everything it sees to search for data quickly and async
/// </summary>
public interface ISearchProvider
{
    string Key { get; } // something kinda "email-gravatar", "domain-dns", "github-user"
    
    bool CanHandle(SearchTarget target); // can he do it??????
    
    IAsyncEnumerable<SearchFinding> ReadAllAsync(SearchTarget target, CancellationToken cancellationToken); // writes that it sees
}