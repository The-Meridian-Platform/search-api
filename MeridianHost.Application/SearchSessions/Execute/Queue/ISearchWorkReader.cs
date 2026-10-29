namespace MeridianHost.Application.SearchSessions.Execute.Queue;

public interface ISearchWorkReader
{
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken);
}