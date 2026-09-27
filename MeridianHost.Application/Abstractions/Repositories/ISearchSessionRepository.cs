using CSharpFunctionalExtensions;
using MeridianHost.Domain.Models;

namespace MeridianHost.Application.Abstractions.Repositories;

public interface ISearchSessionRepository
{
    Task<Result> AddAsync(SearchSession searchSession, CancellationToken cancellationToken);
    Task<Result<SearchSession>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result<List<SearchSession>>> GetAllAsync(CancellationToken cancellationToken);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken);
}