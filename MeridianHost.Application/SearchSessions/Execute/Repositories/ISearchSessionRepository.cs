using CSharpFunctionalExtensions;
using MeridianHost.Domain.SearchFindings;
using MeridianHost.Domain.SearchSessions;

namespace MeridianHost.Application.SearchSessions.Execute.Repositories;

public interface ISearchSessionRepository
{
    Task<Result> AddAsync(SearchSession searchSession, CancellationToken cancellationToken);
    Task<Result> AddFindingAsync(IEnumerable<SearchFinding> searchFinding, CancellationToken cancellationToken);
    Task<Result<SearchSession>> GetByIdAsync(Guid id, bool includeFindings, CancellationToken cancellationToken);
    Task<Result<List<SearchSession>>> GetAllAsync(bool includeFindings, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(SearchSession searchSession, CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}