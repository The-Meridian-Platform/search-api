using CSharpFunctionalExtensions;
using MeridianHost.Application.SearchSessions.Execute.Repositories;
using MeridianHost.Domain.SearchFindings;
using MeridianHost.Domain.SearchSessions;
using MeridianHost.Persistence.SearchFindings.Mappers;
using MeridianHost.Persistence.SearchSessions.Entities;
using MeridianHost.Persistence.SearchSessions.Mappers;
using Microsoft.EntityFrameworkCore;

namespace MeridianHost.Persistence.SearchSessions.Repositories;

public class SearchSessionRepository(AppDbContext dbContext) : ISearchSessionRepository
{
    private readonly AppDbContext _dbContext = dbContext;
    
    public async Task<Result> AddAsync(SearchSession searchSession, CancellationToken cancellationToken)
    {
        await _dbContext.SearchSessions.AddAsync(searchSession.Map(), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> AddFindingAsync(SearchFinding searchFinding, CancellationToken cancellationToken)
    {
        await _dbContext.SearchFindings.AddAsync(searchFinding.Map(), cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<SearchSession>> GetByIdAsync(Guid id, bool includeFindings, CancellationToken cancellationToken)
    {
        IQueryable<SearchSessionEntity> query = _dbContext.SearchSessions
            .AsNoTracking()
            .Include(s => s.Target);

        if (includeFindings)
        {
            query = query.Include(s => s.Findings);
        }

        var result = await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return result is not null
            ? Result.Success(result.Map())
            : Result.Failure<SearchSession>("not found");
    }

    public async Task<Result<List<SearchSession>>> GetAllAsync(bool includeFindings, CancellationToken cancellationToken)
    {
        IQueryable<SearchSessionEntity> query = _dbContext.SearchSessions
            .AsNoTracking()
            .Include(s => s.Target);

        if (includeFindings)
        {
            query = query.Include(s => s.Findings);
        }

        var result = await query.ToListAsync(cancellationToken);
        return Result.Success(result.Select(x => x.Map()).ToList());
    }

    public async Task<Result> UpdateAsync(SearchSession searchSession, CancellationToken cancellationToken)
    {
        var updatedRows = await _dbContext.SearchSessions
            .Where(x => x.Id == searchSession.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.SearchStatus, searchSession.SearchStatus)
                .SetProperty(x => x.StartedAt, searchSession.StartedAt)
                .SetProperty(x => x.FinishedAt, searchSession.FinishedAt),
                cancellationToken);
            
        return updatedRows > 0
            ? Result.Success()
            : Result.Failure<SearchSession>("Search session does not exist");
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var deletedRows = await _dbContext.SearchSessions
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return deletedRows > 0
            ? Result.Success()
            : Result.Failure("Search session does not exist");
    }
}
