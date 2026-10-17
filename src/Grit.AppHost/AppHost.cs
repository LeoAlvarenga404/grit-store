var builder = DistributedApplication.CreateBuilder(args);

// Publish mode writes deploy/compose (README, "Como subir com Docker Compose"); a fixed dashboard port lets the README link it.
builder.AddDockerComposeEnvironment("compose")
    .WithDashboard(dashboard => dashboard.WithHostPort(18888));

// Tags already in the local Docker cache (shared with the integration tests), to spare the Docker disk budget.
var database = builder.AddPostgres("postgres")
    .WithImageTag("18.6-alpine")
    .AddDatabase("grit");

var redis = builder.AddRedis("redis").WithImageTag("8.10.2-alpine");
var storage = builder.AddSeaweedFS("storage").WithImageTag("4.46").WithS3();

// The API seeds this file at start-up in Development; seeding is idempotent, so restarts are safe.
var seedPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "seed", "catalog.json"));

// Generated on the first run and kept in the AppHost user secrets: a fresh clone needs no secret, and none is committed.
var jwtSigningKey = builder.AddParameter("jwt-signing-key", new GenerateParameterDefault { MinLength = 64, Special = false }, secret: true, persist: true);

// Same mechanism: read it with `dotnet user-secrets list --project src/Grit.AppHost` (Parameters:admin-password).
var adminPassword = builder.AddParameter("admin-password", new GenerateParameterDefault { MinLength = 20, Special = false }, secret: true, persist: true);

var api = builder.AddProject<Projects.Grit_Api>("api")
    .WithReference(database)
    .WithEnvironment("Identity__Jwt__SigningKey", jwtSigningKey)
    .WithEnvironment("Identity__Admin__Email", "admin@grit.local")
    .WithEnvironment("Identity__Admin__Password", adminPassword)
    // k6 compares runs with and without the catalog cache: `dotnet run --project src/Grit.AppHost -- --Caching:Enabled=false`.
    .WithEnvironment("Caching__Enabled", builder.Configuration["Caching:Enabled"] ?? "true")
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(storage)
    .WaitFor(storage)
    // The compose image has no checkout: Grit.Api.csproj publishes the seed next to the API, in the image's /app.
    .WithEnvironment("Seed__Path", builder.ExecutionContext.IsPublishMode ? "/app/seed/catalog.json" : seedPath)
    // Generated products for k6, off unless asked: `dotnet run --project src/Grit.AppHost -- --Seed:VolumeProducts=50000`.
    .WithEnvironment("Seed__VolumeProducts", builder.Configuration["Seed:VolumeProducts"] ?? "0")
    // Compose runs the API in Development too: migrations and seeds only run there (ADR 0008); a real deploy would not.
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithHttpHealthCheck("/health")
    // Compose waits for PostgreSQL to start, not to accept connections: a start that fails on the migrations is retried.
    .PublishAsDockerComposeService((_, service) => service.Restart = "unless-stopped");

// The fake gateway runs until Stripe test keys are stored in these user secrets (README, "Como ligar o Stripe").
// Run mode only: the generated compose file never carries them.
if (builder.ExecutionContext.IsRunMode && builder.Configuration["Payments:Provider"] is { } paymentsProvider)
{
    api.WithEnvironment("Payments__Provider", paymentsProvider)
        .WithEnvironment("Payments__Stripe__SecretKey", builder.Configuration["Payments:Stripe:SecretKey"] ?? string.Empty)
        .WithEnvironment("Payments__Stripe__WebhookSecret", builder.Configuration["Payments:Stripe:WebhookSecret"] ?? string.Empty);
}

builder.AddNextJsApp("web", "../../web")
    .WithNpm(installCommand: "ci")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitFor(api)
    // A fixed host port for the browser; 8000 is the container port Aspire gives the Next.js server (PORT).
    .PublishAsDockerComposeService((_, service) => service.Ports = ["3000:8000"]);

builder.Build().Run();
