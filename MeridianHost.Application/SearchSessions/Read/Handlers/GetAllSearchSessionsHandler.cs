using CSharpFunctionalExtensions;
using MeridianHost.Application.SearchSessions.Execute.Repositories;
using MeridianHost.Application.SearchSessions.Read.Contracts;
using MeridianHost.Application.SearchSessions.Read.Mappers;

namespace MeridianHost.Application.SearchSessions.Read.Handlers;

public class GetAllSearchSessionsHandler(ISearchSessionRepository repository)
{
    private readonly ISearchSessionRepository _repository = repository;

    public async Task<Result<List<SearchSessionResponse>>> HandleAsync(bool? includeFindings, CancellationToken cancellationToken)
    {
        var result = await _repository.GetAllAsync(includeFindings.HasValue, cancellationToken);

        return result.IsSuccess
            ? result.Value.Select(x => x.ToResponse()).ToList()
            : Result.Failure<List<SearchSessionResponse>>(result.Error);
    }
}