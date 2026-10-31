using System.Threading.Channels;
using MeridianHost.Application.SearchSessions.Execute.Providers;
using MeridianHost.Application.SearchSessions.Execute.Providers.Contracts;
using MeridianHost.Application.SearchSessions.Execute.Queue;
using MeridianHost.Application.SearchSessions.Execute.Repositories;
using MeridianHost.Domain.SearchFindings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MeridianHost.Infrastructure.SearchSessions.Workers;

/// <summary>
/// The engine of the whole project ^^
///
/// In the latest update, the engine has moved from consistency to concurrency
/// </summary>
/// <param name="scopeFactory"></param>
/// <param name="queue"></param>
/// <param name="providers"></param>
/// <param name="logger"></param>
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

        var sessionResult = await repository.GetByIdAsync(searchSessionId, false, cancellationToken);
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
        {
            _logger.LogWarning("Providers for this target not found :(. Session id: {SearchSessionId}",
                searchSessionId);
            session.Finish();
            await repository.UpdateAsync(session, cancellationToken);
            return;
        }

        // Create unbounded channel 
        var channel = Channel.CreateUnbounded<(string providerKey, RawSearchFinding finding)>(
            new UnboundedChannelOptions
            {
                SingleWriter = false, // Any providers
                SingleReader = true // Single reader: Database, etc
            });

        // We launch all providers COMPETITIVELY in the background
        var producersTask = Task.Run(async () =>
        {
            var tasks = matchedProviders.Select(async provider =>
            {
                try
                {
                    _logger.LogInformation("Provider Launch: {Key}", provider.Key);

                    await foreach (var finding in provider.ReadAllAsync(session.Target, cancellationToken))
                    {
                        await channel.Writer.WriteAsync((provider.Key, finding), cancellationToken);
                    }

                    _logger.LogInformation("Provider finished {Key}", provider.Key);
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provider {Key} failed with exception", provider.Key);
                }
            });
            
            await Task.WhenAll(tasks);
            
            channel.Writer.Complete();
        }, cancellationToken);
        
        // The only consumer: he proofreads the finds as they are received and saves them in the database
        var findings = new List<SearchFinding>();
        
        await foreach (var (providerKey, rawFinding) in channel.Reader.ReadAllAsync(cancellationToken))
        {
            _logger.LogInformation("[{Key}] found ({Type}): {Data}", providerKey, rawFinding.Type, rawFinding.DataJson);

            var findingModel = SearchFinding.Create(
                searchSessionId,
                providerKey,
                rawFinding.Type,
                rawFinding.DataJson);
            if (findingModel.IsFailure)
            {
                _logger.LogWarning("Failed to create search finding: {Error}", findingModel.Error);
                continue; // we do not interrupt the session because of one broken element
            }
            
            session.AddFinding(findingModel.Value);
            findings.Add(findingModel.Value);
        }

        await producersTask;

        if (findings.Count > 0)
            await repository.AddFindingAsync(findings, cancellationToken);
        
        session.Finish();
        await repository.UpdateAsync(session, cancellationToken);
        
        _logger.LogInformation("Session {SessionId} successfully finished", searchSessionId);
    }
}