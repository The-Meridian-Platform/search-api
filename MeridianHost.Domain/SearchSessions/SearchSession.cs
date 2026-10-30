using CSharpFunctionalExtensions;
using MeridianHost.Domain.SearchFindings;
using MeridianHost.Domain.SearchSessions.Enum;
using MeridianHost.Domain.SearchTargets;

namespace MeridianHost.Domain.SearchSessions;

public sealed class SearchSession
{
    public Guid Id { get; private set; }
    public SearchTarget Target { get; private set; }
    public IReadOnlyCollection<SearchFinding> Findings => _findings.AsReadOnly();
    public Status SearchStatus { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }

    private readonly List<SearchFinding> _findings = [];

    private SearchSession(Guid id, SearchTarget target, IReadOnlyCollection<SearchFinding> findings,
        Status searchStatus,
        DateTimeOffset createdAt, DateTimeOffset? startedAt, DateTimeOffset? finishedAt)
    {
        Id = id;
        Target = target;
        _findings = findings.ToList();
        SearchStatus = searchStatus;
        CreatedAt = createdAt;
        StartedAt = startedAt;
        FinishedAt = finishedAt;
    }

    public static Result<SearchSession> Create(SearchTarget target)
    {
        return Result.Success(new SearchSession(Guid.NewGuid(), target, [], Status.Pending, DateTimeOffset.UtcNow, null,
            null));
    }

    public static SearchSession Load(Guid id, SearchTarget target, IReadOnlyCollection<SearchFinding> findings,
        Status searchStatus, DateTimeOffset createdAt,
        DateTimeOffset? startedAt, DateTimeOffset? finishedAt)
        => new SearchSession(id, target, findings, searchStatus, createdAt, startedAt, finishedAt);

    public void AddFinding(SearchFinding finding)
    {
        _findings.Add(finding);
    }

    public void Start()
    {
        StartedAt = DateTimeOffset.UtcNow;
        SearchStatus = Status.Running;
    }

    public void Cancel()
    {
        SearchStatus = Status.Cancelled;
        FinishedAt = DateTimeOffset.UtcNow;
    }

    public void Finish()
    {
        FinishedAt = DateTimeOffset.UtcNow;
        SearchStatus = Status.Completed;
    }

    public void Fail()
    {
        SearchStatus = Status.Failed;
        FinishedAt = DateTimeOffset.UtcNow;
    }
}