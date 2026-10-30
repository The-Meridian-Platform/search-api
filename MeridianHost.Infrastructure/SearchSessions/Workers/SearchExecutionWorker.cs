using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Queue;
using MeridianHost.Application.SearchSessions.Execute.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeridianHost.Infrastructure.SearchSessions.Workers;

public class SearchExecutionWorker(
    IServiceScopeFactory scopeFactory,
    ISearchWorkQueue queue,
    IEnumerable<ISearchProvider> providers,
    ILogger<SearchExecutionWorker> logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ISearchWorkQueue _queue = queue;
    private readonly IEnumerable<ISearchProvider> _providers = providers;
    private readonly ILogger<SearchExecutionWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background Service is starting...");

        await foreach (var sessionId in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessSearchSessionAsync(sessionId, stoppingToken);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during session processing. Session id: {SessionId}", sessionId);
            }
        }
    }

    private async Task ProcessSearchSessionAsync(Guid searchSessionId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISearchSessionRepository>();

        var sessionResult = await repository.GetByIdAsync(searchSessionId, cancellationToken);
        if (sessionResult.IsFailure)
        {
            _logger.LogWarning("Session {SearchSessionId} not found", searchSessionId);
            return;
        }

        var session = sessionResult.Value;

        session.Start();

        await repository.UpdateAsync(session, cancellationToken);
        _logger.LogInformation("Finding for search session {SearchSessionId} has started. Target: {Target}",
            searchSessionId, session.Target.Value);

        var matchedProviders = _providers.Where(p => p.CanHandle(session.Target)).ToList();
        if (!matchedProviders.Any())
            _logger.LogWarning("Providers for this target not found :(. Session id: {SearchSessionId}",
                searchSessionId);

        foreach (var provider in matchedProviders)
        {
            _logger.LogInformation("Provider launch: {Key}", provider.Key);

            await foreach (var finding in provider.ReadAllAsync(session.Target, cancellationToken))
            {
                _logger.LogInformation("[{Key}] found ({Type}): {Data}", provider.Key, finding.Type, finding.DataJson);
            }
        }
        
        session.Finish();
        await repository.UpdateAsync(session, cancellationToken);
        
        _logger.LogInformation("Session {SessionId} successfully finished", searchSessionId);
    }
}