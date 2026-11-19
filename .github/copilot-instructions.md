# Copilot instructions

## Build, test, and lint

The solution targets .NET 11 and is defined by `Evently.slnx`.

```bash
dotnet restore Evently.slnx
dotnet build Evently.slnx --configuration Release --no-restore
dotnet test Evently.slnx --configuration Release
```

The full test command includes Testcontainers-based integration tests, so Docker must be running.

Run the Aspire application host for local development:

```bash
dotnet run --project Evently.AppHost
```

The AppHost provisions PostgreSQL, Redis, RabbitMQ, Keycloak, and MongoDB before starting `Evently.Api`.

To run one test project, pass its project file; for example:

```bash
dotnet test src/Modules/Events/test/Evently.Modules.Events.UnitTests/Evently.Modules.Events.UnitTests.csproj
```

To run a single test, add a filter by fully qualified name:

```bash
dotnet test src/Modules/Events/test/Evently.Modules.Events.UnitTests/Evently.Modules.Events.UnitTests.csproj \
  --filter "FullyQualifiedName~Evently.Modules.Events.UnitTests.Events.EventTests.Create_ShouldReturnFailure_WhenEndDatePrecedesStartDate"
```

There is no separate lint command in the repository. `dotnet build` runs the configured .NET and Sonar analyzers, and code-style violations and warnings are treated as build errors. Integration tests use Testcontainers and require Docker.

## Architecture

Evently is a .NET modular monolith orchestrated with .NET Aspire. `Evently.AppHost` defines the local services and dependencies; `src/API/Evently.Api` is the composition root that registers shared infrastructure and the business modules, configures Wolverine messaging, and maps discovered endpoints.

The business modules are Events, Users, Ticketing, and Attendance. Each is divided into Domain, Application, Infrastructure, IntegrationEvents, and Presentation projects, with unit, integration, and architecture tests under the module’s `test` directory. `src/Common` contains shared domain primitives, application messaging and behaviors, infrastructure services, and presentation helpers. Keep dependencies pointed inward: Domain must not depend on Application or Infrastructure, Application must not depend on Infrastructure or Presentation, and Presentation must not depend on Infrastructure. Architecture tests encode these boundaries.

Module infrastructure owns its persistence and dependency-registration details. Cross-module communication uses integration-event contracts and handlers rather than reaching into another module’s domain. Domain events raised by entities are collected by the shared domain base; EF Core’s save interceptor persists them to the outbox, and inbox/outbox processing provides durable, idempotent message handling.

`Evently.Api/Program.cs` is the composition root only; feature-specific Wolverine, EF Core, repository, inbox/outbox, and endpoint registration belongs in the module’s infrastructure `*Module` extension.

The AppHost is the source of local infrastructure names and connection strings. The API reads the `evently-db`, `evently-cache`, `evently-queue`, and module configuration sections through Aspire-provided configuration.

## Codebase conventions

- Implement a feature across the existing layers: application commands/queries and handlers, domain behavior, infrastructure persistence as needed, and presentation endpoints. Follow the corresponding feature’s folder structure and naming.
- Commands and queries are message types handled through `ICommandHandler` / `IQueryHandler`; handlers return the shared `Result` types. Keep business invariants in domain entities and represent expected failures with `Result` and the module’s `*Errors`.
- Presentation routes are discovered from `IEndpoint` implementations. Endpoints invoke application handlers and map `Result` using the shared presentation result helpers; retain module permission and tag conventions.
- Keep module registration in that module’s infrastructure registration class and use the module’s existing configuration and database-schema patterns. Put contracts intended for inter-module messages in the module’s `IntegrationEvents` project.
- Unit tests cover domain behavior; integration tests exercise registered handlers and persistence with the module’s `WebApplicationFactory`/Testcontainers setup; architecture tests protect layer boundaries. Use the existing xUnit, Bogus, and AwesomeAssertions patterns.
- Follow `.editorconfig`: file-scoped namespaces, four-space indentation, explicit types except where the type is apparent, and the configured C# style rules. `Directory.Build.props` enables nullable references and implicit usings and treats warnings as errors.
- Validate business rules in Domain factories/methods and represent expected failures through `Result` plus module `*Errors` types. Application handlers should return `Result` rather than throwing for expected failures.
- Wolverine command/query handlers and FluentValidation validators are discovered by the shared application registration. Preserve the existing handler, validator, record, and feature-folder naming and decorator pipeline.
- Presentation routes implement `IEndpoint`, use minimal-api route maps, retain module permission and tag conventions, and convert `Result` through `Result.Match(Results.Ok, ApiResults.Problem)`.
- EF Core module contexts use module-owned schemas and snake-case naming. Add database changes through the module migration conventions rather than modifying other modules’ contexts or schemas.
- Integration events and domain events are distinct: integration events cross module boundaries; domain events are raised by entities and handled inside/outside the module through the outbox pipeline. Idempotent inbox/outbox decorators are applied by module infrastructure registration.
