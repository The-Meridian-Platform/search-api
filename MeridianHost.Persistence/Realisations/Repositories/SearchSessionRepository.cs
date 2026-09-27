using CSharpFunctionalExtensions;
using MeridianHost.Application.Abstractions.Repositories;
using MeridianHost.Domain.Models;
using MeridianHost.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace MeridianHost.Persistence.Realisations.Repositories;

public class SearchSessionRepository(AppDbContext dbContext) : ISearchSessionRepository
{
    private readonly AppDbContext _dbContext = dbContext;
    
    public async Task<Result> AddAsync(SearchSession searchSession, CancellationToken cancellationToken)
    {
        await _dbContext.SearchSessions.AddAsync(searchSession.Map(), cancellationToken);

        return Result.Success();
    }

    public async Task<Result<SearchSession>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dbContext.SearchSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return result is not null
            ? Result.Success(result.Map())
            : Result.Failure<SearchSession>("not found");
    }

    public async Task<Result<List<SearchSession>>> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await _dbContext.SearchSessions
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        
        return result.Count != 0
            ? Result.Success(result.Select(x => x.Map()).ToList())
            : Result.Failure<List<SearchSession>>("not found");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await _dbContext.SearchSessions.Where(x => x.Id == id).ExecuteDeleteAsync(cancellationToken);

        return Result.Success();
    }
}