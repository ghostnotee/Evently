using Evently.Api.Extensions;
using Evently.Common.Application;
using Evently.Modules.Events.Infrastructure;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi(options =>
{
    options.CreateSchemaReferenceId = typeInfo =>
    {
        string? defaultSchemaId = Microsoft.AspNetCore.OpenApi.OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
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


builder.Services.AddApplication([Evently.Modules.Events.Application.AssemblyReference.Assembly]);

builder.Services.AddEventsModule(builder.Configuration);

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
