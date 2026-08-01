using Projects; 

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> username = builder.AddParameter("postgres-user", "postgres", secret: true);
IResourceBuilder<ParameterResource> password = builder.AddParameter("postgres-pass", "postgres", secret: true);
IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("evently-postgres", username, password)
    .WithImage("postgres", "18")
    .WithEndpoint(name: "postgres-endpoint", scheme: "tcp", port: 5432, targetPort: 5432, isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false);
IResourceBuilder<PostgresDatabaseResource> eventlyDb = postgres.AddDatabase("evently-db");

IResourceBuilder<ParameterResource> redisPassword = builder.AddParameter("redis-pass", "redis", secret: true);
IResourceBuilder<RedisResource> redis = builder
    .AddRedis("evently-redis", password: redisPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithImage("redis", "8")
    .WithEndpoint(name: "redis-endpoint", scheme: "tcp", port: 6379, targetPort: 6379, isProxied: false)
    .WithDataVolume(isReadOnly: false);

IResourceBuilder<ParameterResource> keycloakUsername = builder.AddParameter("keycloak-admin-user");
IResourceBuilder<ParameterResource> keycloakPassword = builder.AddParameter("keycloak-admin-password", true);
IResourceBuilder<ContainerResource> keycloak = builder
    .AddKeycloak("evently-keycloak", adminUsername: keycloakUsername, adminPassword: keycloakPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(name: "http", scheme: "http", port: 8080, targetPort: 8080, isProxied: false)
    .WithDataBindMount("../.files")
    .WithRealmImport("../.files")
    .WithOtlpExporter();

// IResourceBuilder<ParameterResource> rabbitmqUsername = builder.AddParameter("username", "guest", secret: true);
// IResourceBuilder<ParameterResource> rabbitmqPassword = builder.AddParameter("password", "guest", secret: true);
// IResourceBuilder<RabbitMQServerResource> rabbitmq = builder.AddRabbitMQ("messaging", rabbitmqUsername, rabbitmqPassword)
//     .WithLifetime(ContainerLifetime.Persistent)
//     .WithDataVolume(isReadOnly: false)
//     .WithEndpoint(name: "rabbitmq-endpoint", scheme: "tcp", port: 5672, targetPort: 5672, isProxied: false)
//     .WithManagementPlugin(15672)
//     .WithEndpoint(name: "rabbitmq-management", scheme: "http", port: 15672, targetPort: 15672, isProxied: false);

builder.AddProject<Evently_Api>("evently-api")
    .WithReference(eventlyDb)
    .WaitFor(eventlyDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(keycloak.GetEndpoint("http"))
    .WaitFor(keycloak);
// .WithReference(rabbitmq)
// .WaitFor(rabbitmq);

await builder.Build().RunAsync();
