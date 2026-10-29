using CSharpFunctionalExtensions;
using MeridianHost.Application.Abstractions.Repositories;

namespace MeridianHost.Application.SearchSessions.Delete.Handlers;

public class DeleteSearchSessionHandler(ISearchSessionRepository repository)
{
    private readonly ISearchSessionRepository _repository = repository;

    public async Task<Result> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _repository.DeleteAsync(id, cancellationToken);

        return result.IsSuccess
            ? Result.Success()
            : Result.Failure(result.Error);
    }
}