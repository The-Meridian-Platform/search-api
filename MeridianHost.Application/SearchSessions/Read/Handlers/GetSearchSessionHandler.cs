using CSharpFunctionalExtensions;
using MeridianHost.Application.Abstractions.Repositories;
using MeridianHost.Application.SearchSessions.Read.Contracts;
using MeridianHost.Application.SearchSessions.Read.Mappers;

namespace MeridianHost.Application.SearchSessions.Read.Handlers;

public class GetSearchSessionHandler(ISearchSessionRepository repository)
{
    private readonly ISearchSessionRepository _repository = repository;

    public async Task<Result<SearchSessionResponse>> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repository.GetByIdAsync(id, cancellationToken);

        return result.IsSuccess
            ? result.Value.ToResponse()
            : Result.Failure<SearchSessionResponse>(result.Error);
    }
}