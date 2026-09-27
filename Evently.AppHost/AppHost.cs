using Projects;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> username = builder.AddParameter("postgres-user", "postgres", secret: true);
IResourceBuilder<ParameterResource> password = builder.AddParameter("postgres-pass", "postgres", secret: true);
IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("evently-postgres", username, password)
    .WithImageTag("18.6")
    .WithEndpoint(name: "postgres-endpoint", scheme: "tcp", port: 5432, targetPort: 5432, isProxied: false)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume(isReadOnly: false);
IResourceBuilder<PostgresDatabaseResource> eventlyDb = postgres.AddDatabase("evently-db");

IResourceBuilder<ParameterResource> redisPassword = builder.AddParameter("redis-pass", "redis", secret: true);
IResourceBuilder<RedisResource> redis = builder
    .AddRedis("evently-cache", password: redisPassword)
    .WithImageTag("8.10")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(name: "redis-endpoint", scheme: "tcp", port: 6379, targetPort: 6379, isProxied: false)
    .WithDataVolume(isReadOnly: false);

IResourceBuilder<ParameterResource> keycloakUsername = builder.AddParameter("keycloak-admin-user");
IResourceBuilder<ParameterResource> keycloakPassword = builder.AddParameter("keycloak-admin-password", true);
IResourceBuilder<ContainerResource> keycloak = builder
    .AddKeycloak("evently-keycloak", adminUsername: keycloakUsername, adminPassword: keycloakPassword)
    .WithImageTag("26.7")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(name: "keycloak-endpoint", scheme: "https", port: 18080, targetPort: 8443, isProxied: false)
    .WithEndpoint(name: "keycloak-health-endpoint", scheme: "https", port: 9000, targetPort: 9000, isProxied: false)
    .WithDataBindMount("../.files")
    .WithRealmImport("../.files/evently-realm-export.json")
    .WithOtlpExporter();

IResourceBuilder<ParameterResource> rabbitmqUsername = builder.AddParameter("username", "guest", secret: true);
IResourceBuilder<ParameterResource> rabbitmqPassword = builder.AddParameter("password", "guest", secret: true);
IResourceBuilder<RabbitMQServerResource> rabbitmq = builder.AddRabbitMQ("evently-queue", rabbitmqUsername, rabbitmqPassword)
    .WithImageTag("4.3-management-alpine")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataBindMount("../.containers/queue/data")
    .WithBindMount("../.containers/queue/log", "/var/log/rabbitmq")
    .WithManagementPlugin(15672);

builder.AddProject<Evently_Api>("evently-api")
    .WithReference(eventlyDb)
    .WaitFor(eventlyDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(keycloak.GetEndpoint("keycloak-endpoint"))
    .WaitFor(keycloak)
    .WithReference(rabbitmq)
    .WaitFor(rabbitmq);

await builder.Build().RunAsync();
