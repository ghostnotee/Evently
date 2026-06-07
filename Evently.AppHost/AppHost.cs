using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> username = builder.AddParameter("postgres-user", "postgres", secret: true);
IResourceBuilder<ParameterResource> password = builder.AddParameter("postgres-pass", "postgres", secret: true);
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("evently-postgres", username, password)
    .WithImage("postgres", "18")
    .WithEndpoint(name: "postgres-endpoint", scheme: "tcp", port: 5432, targetPort: 5432, isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false);
IResourceBuilder<PostgresDatabaseResource> eventlyDb = postgres.AddDatabase("evently-db");

IResourceBuilder<ParameterResource> redisPassword = builder.AddParameter("redis-pass", "redis", secret: true);
IResourceBuilder<RedisResource> redis = builder.AddRedis("evently-redis", password: redisPassword)
    .WithImage("redis", "8")
    .WithEndpoint(name: "redis-endpoint", scheme: "tcp", port: 6379, targetPort: 6379, isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false);


builder.AddProject<Evently_Api>("evently-api")
    .WithReference(eventlyDb)
    .WithReference(redis)
    .WaitFor(eventlyDb)
    .WaitFor(redis);

await builder.Build().RunAsync();
