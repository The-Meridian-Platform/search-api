using Grpc.Core;
using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Contracts.Additional;
using MeridianHost.Application.SearchSessions.Execute.Broadcasting.Repositories;

namespace MeridianHost.Api.Grpc.Realisations;

public class SearchStreamGrpcService(
    ISearchSessionBroadcaster broadcaster,
    ILogger<SearchStreamGrpcService> logger) : SearchStreamService.SearchStreamServiceBase
{
    private readonly ISearchSessionBroadcaster _broadcaster = broadcaster;
    private readonly ILogger<SearchStreamGrpcService> _logger = logger;

    public override async Task SubscribeToSession(
        SubscribeSessionRequest request,
        IServerStreamWriter<SearchStreamResponse> responseStream,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.SessionId, out var sessionId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "SessionId is not valid"));

        _logger.LogInformation("Client subscribed to gRPC session stream {SessionId}", sessionId);

        try
        {
            await foreach (var evt in _broadcaster.SubscribeAsync(sessionId, context.CancellationToken))
            {
                var response = new SearchStreamResponse
                {
                    SessionId = sessionId.ToString()
                };

                switch (evt)
                {
                    case SessionStartedEvent started:
                        response.SessionStarted = new SessionStartedPayload
                        {
                            TargetValue = started.TargetValue,
                            StartedAt = started.StartedAt.ToString("O")
                        };
                        break;
                    
                    case FindingDiscoveredEvent finding:
                        response.FindingDiscovered = new FindingDiscoveredPayload
                        {
                            FindingId = finding.FindingId.ToString(),
                            ProviderKey = finding.ProviderKey,
                            Type = finding.Type,
                            DataJson = finding.DataJson,
                            CreatedAt = finding.CreatedAt.ToString("O")
                        };
                        break;
                    case SessionFinishedEvent finished:
                        response.SessionFinished = new SessionFinishedPayload
                        {
                            Status = finished.Status,
                            FinishedAt = finished.FinishedAt.ToString("O"),
                            TotalFindings = finished.TotalFindings
                        };
                        break;
                }
                
                await responseStream.WriteAsync(response);
            }
        }
        
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Client disconnected from session {SessionId}", sessionId);
        }
    }
}