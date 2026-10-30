using CSharpFunctionalExtensions;
using MeridianHost.Domain.SearchSessions;

namespace MeridianHost.Application.SearchSessions.Execute.Repositories;

public interface ISearchSessionRepository
{
    Task<Result> AddAsync(SearchSession searchSession, CancellationToken cancellationToken);
    Task<Result<SearchSession>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<SearchSession>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result> UpdateAsync(SearchSession searchSession, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}