using Evently.Api.Extensions;
using Evently.Common.Application;
using Evently.Common.Infrastructure;
using Evently.Modules.Events.Application;
using Evently.Modules.Events.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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


builder.Services.AddApplication([AssemblyReference.Assembly]);
builder.AddInfrastructure();
builder.Configuration.AddModuleConfiguration(["events"]);

builder.AddEventsModule();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.ApplyMigrations();
}

EventsModule.MapEndpoints(app);

await app.RunAsync();
