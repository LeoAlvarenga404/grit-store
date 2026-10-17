var builder = DistributedApplication.CreateBuilder(args);

// Tags already in the local Docker cache (shared with the integration tests), to spare the Docker disk budget.
var database = builder.AddPostgres("postgres")
    .WithImageTag("18.6-alpine")
    .AddDatabase("grit");

var redis = builder.AddRedis("redis").WithImageTag("8.10.2-alpine");
// Declared now, referenced by the API when product images (S3) arrive.
builder.AddSeaweedFS("storage").WithImageTag("4.46").WithS3();

// The API seeds this file at start-up in Development; seeding is idempotent, so restarts are safe.
var seedPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "seed", "catalog.json"));

// Generated on the first run and kept in the AppHost user secrets: a fresh clone needs no secret, and none is committed.
var jwtSigningKey = builder.AddParameter("jwt-signing-key", new GenerateParameterDefault { MinLength = 64, Special = false }, secret: true, persist: true);

var api = builder.AddProject<Projects.Grit_Api>("api")
    .WithReference(database)
    .WithEnvironment("Identity__Jwt__SigningKey", jwtSigningKey)
    .WaitFor(database)
    .WithReference(redis)
    .WaitFor(redis)
    .WithEnvironment("Seed__Path", seedPath)
    .WithHttpHealthCheck("/health");

builder.AddJavaScriptApp("web", "../../web", "dev")
    .WithNpm(installCommand: "ci")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
