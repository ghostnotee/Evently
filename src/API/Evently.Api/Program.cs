using Evently.Modules.Events.Api;
using Evently.Modules.Events.Api.Database;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();

builder.AddNpgsqlDbContext<EventsDbContext>("evently");

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

EventsModule.MapEndpoints(app);

await app.RunAsync();
