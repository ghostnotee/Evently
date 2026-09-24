using Evently.Common.Application;
using Evently.Common.Infrastructure;
using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Ticketing.Application;
using Evently.Modules.Ticketing.Infrastructure;
using Evently.Ticketing.Api.Extensions;
using Evently.Ticketing.Api.Middleware;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration)
        .WriteTo.OpenTelemetry());

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.AddServiceDefaults();

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = typeInfo =>
    {
        string? defaultSchemaId = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
        if (defaultSchemaId is null)
        {
            return null;
        }

        string? typeNamespace = typeInfo.Type.Namespace;
        return string.IsNullOrWhiteSpace(typeNamespace)
            ? defaultSchemaId
            : $"{typeNamespace}.{defaultSchemaId}";
    };
});

builder.Services.AddApplication([
    AssemblyReference.Assembly
]);

string connectionStringCache = builder.Configuration.GetConnectionString("evently-redis")!;
builder.AddInfrastructure(
    builder.Environment.ApplicationName,
    [
        TicketingModule.ConfigureConsumers
    ]);

builder.Configuration.AddModuleConfiguration(["ticketing"]);
builder.AddTicketingModule();

builder.Services.AddHealthChecks()
    .AddRedis(connectionStringCache)
    .AddUrlGroup(new Uri(builder.Configuration["Keycloak:HealthUrl"]!), HttpMethod.Get, "keycloak");


WebApplication app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.ApplyMigrations();
}

app.MapEndpoints();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseSerilogRequestLogging();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

await app.RunAsync();

public partial class Program;
