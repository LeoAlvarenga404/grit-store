var builder = DistributedApplication.CreateBuilder(args);

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
    .WithEnvironment("Seed__Path", seedPath)
    .WithHttpHealthCheck("/health");

builder.AddJavaScriptApp("web", "../../web", "dev")
    .WithNpm(installCommand: "ci")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
