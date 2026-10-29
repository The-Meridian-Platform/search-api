using MeridianHost.Domain.SearchTargets;

namespace MeridianHost.Application.SearchSessions.Execute.Providers;

public interface ISearchProvider
{
    string Key { get; }
    
    bool CanHandle(SearchTarget target);
    
    IAsyncEnumerable<Guid> ReadAllAsync(SearchTarget target, CancellationToken cancellationToken);
}