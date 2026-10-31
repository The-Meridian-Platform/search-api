using System.Text.Json.Serialization;
using MeridianHost.Api.Common;
using MeridianHost.Api.Endpoints;
using MeridianHost.Api.Grpc.Realisations;
using MeridianHost.Application.DI;
using MeridianHost.Infrastructure.DI;
using MeridianHost.Persistence;
using MeridianHost.Persistence.DI;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddPersistence(configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.MapGrpcService<SearchStreamGrpcService>();

app.MapSearchSessionsEndpoints();

app.Run();
