using MeridianHost.Application.SearchSessions.Create.Contracts;
using MeridianHost.Application.SearchSessions.Create.Handlers;
using MeridianHost.Application.SearchSessions.Delete.Handlers;
using MeridianHost.Application.SearchSessions.Read.Handlers;

namespace MeridianHost.Api.Endpoints;

public static class SearchSessions
{
    public static IEndpointRouteBuilder MapSearchSessionsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/search-sessions");

        group.MapPost("/", async (
            CreateSearchSessionRequest request,
            CreateSearchSessionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            return result.IsSuccess
                ? Results.Created($"/api/search-sessions/{result.Value.SearchSessionId}", result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/", async (
            bool? includeFindings,
            GetAllSearchSessionsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(includeFindings, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            bool? includeFindings,
            GetSearchSessionHandler handler, 
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, includeFindings, cancellationToken);
            
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(result.Error);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            DeleteSearchSessionHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(id, cancellationToken);
            
            return result.IsSuccess
                ? Results.NoContent()
                :  Results.BadRequest(result.Error);
        });

        return group;
    }
}