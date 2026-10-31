using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Contracts;

namespace MeridianHost.Application.SearchSessions.Execute.Broadcasting.Repositories;

public interface ISearchSessionBroadcaster
{
    ValueTask PublishAsync(SearchSessionEvent searchSessionEvent, CancellationToken cancellationToken = default);
    IAsyncEnumerable<SearchSessionEvent> SubscribeAsync(Guid sessionId, CancellationToken cancellationToken = default);
}