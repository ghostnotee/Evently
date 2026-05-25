IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Evently_Api>("evently-api");

await builder.Build().RunAsync();
