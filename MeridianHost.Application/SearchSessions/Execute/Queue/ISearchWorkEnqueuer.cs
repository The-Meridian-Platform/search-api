namespace MeridianHost.Application.SearchSessions.Execute.Queue;

public interface ISearchWorkEnqueuer
{
    ValueTask EnqueueAsync(Guid searchSessionId, CancellationToken cancellationToken);
}