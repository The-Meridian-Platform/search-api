using MeridianHost.Application.DI;
using MeridianHost.Infrastructure.DI;
using MeridianHost.Persistence.DI;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddPersistence(configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();