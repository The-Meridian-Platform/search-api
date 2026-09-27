using CSharpFunctionalExtensions;
using MeridianHost.Domain.Enums;

namespace MeridianHost.Domain.Models;

public sealed class SearchSession
{
    public Guid Id { get; private set; }
    public SearchTarget Target { get; private set; }
    public Status SearchStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }

    private SearchSession(Guid id, SearchTarget target, Status searchStatus, DateTimeOffset createdAt, DateTimeOffset? startedAt, DateTimeOffset? finishedAt)
    {
        Id = id;
        Target = target;
        SearchStatus = searchStatus;
        CreatedAt = createdAt;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }

    public static Result<SearchSession> Create(SearchTarget target)
    {
        return Result.Success(new SearchSession(Guid.NewGuid(), target, Status.Pending, DateTimeOffset.UtcNow, null, null));
    }
}