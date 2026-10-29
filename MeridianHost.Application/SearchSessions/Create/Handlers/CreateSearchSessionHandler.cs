using CSharpFunctionalExtensions;
using FluentValidation;
using MeridianHost.Application.SearchSessions.Create.Contracts;
using MeridianHost.Application.SearchSessions.Execute.Repositories;
using MeridianHost.Domain.SearchSessions;
using MeridianHost.Domain.SearchTargets;

namespace MeridianHost.Application.SearchSessions.Create.Handlers;

public class CreateSearchSessionHandler(
    ISearchSessionRepository repository,
    IValidator<CreateSearchSessionRequest> validator)
{
    private readonly ISearchSessionRepository _repository = repository;
    private readonly IValidator<CreateSearchSessionRequest> _validator = validator;
    
    public async Task<Result<CreateSearchSessionResponse>> HandleAsync(
        CreateSearchSessionRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return Result.Failure<CreateSearchSessionResponse>(
                string.Join("; ", validationResult.Errors.Select(x => x.ErrorMessage)));

        var searchTarget = SearchTarget.Create(request.Value, request.TargetType);
        if (searchTarget.IsFailure)
            return Result.Failure<CreateSearchSessionResponse>(searchTarget.Error);

        var searchSession = SearchSession.Create(searchTarget.Value);
        if (searchSession.IsFailure)
            return Result.Failure<CreateSearchSessionResponse>(searchSession.Error);

        await _repository.AddAsync(searchSession.Value, cancellationToken);

        return Result.Success(new CreateSearchSessionResponse(searchSession.Value.Id));
    }
}