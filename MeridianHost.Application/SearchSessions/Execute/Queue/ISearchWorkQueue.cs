namespace MeridianHost.Application.SearchSessions.Execute.Queue;

public interface ISearchWorkQueue
{
    ValueTask EnqueueAsync(Guid searchSessionId, CancellationToken cancellationToken);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}