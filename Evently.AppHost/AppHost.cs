IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> username = builder.AddParameter("postgres-user", "postgres", secret: true);
IResourceBuilder<ParameterResource> password = builder.AddParameter("postgres-pass", "postgres", secret: true);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("Database", username, password)
    .WithImage("postgres", "18")
    .WithHostPort(5432)
    .WithEndpoint(name: "postgres-endpoint", scheme: "tcp", port: 5432, targetPort: 5432, isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false)
    .WithContainerName("evently-postgres");

IResourceBuilder<PostgresDatabaseResource> eventlyDb = postgres.AddDatabase("evently");

builder.AddProject<Projects.Evently_Api>("evently-api")
    .WithReference(eventlyDb)
    .WaitFor(eventlyDb);

await builder.Build().RunAsync();
