var builder = DistributedApplication.CreateBuilder(args);

// Tags already in the local Docker cache (shared with the integration tests), to spare the Docker disk budget.
var database = builder.AddPostgres("postgres")
    .WithImageTag("18.6-alpine")
    .AddDatabase("grit");

// Declared now, referenced by the API when Basket (Redis) and product images (S3) arrive.
builder.AddRedis("redis").WithImageTag("8.10.2-alpine");
builder.AddSeaweedFS("storage").WithImageTag("4.46").WithS3();

// The API seeds this file at start-up in Development; seeding is idempotent, so restarts are safe.
var seedPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", "seed", "catalog.json"));

var api = builder.AddProject<Projects.Grit_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithEnvironment("Seed__Path", seedPath)
    .WithHttpHealthCheck("/health");

builder.AddJavaScriptApp("web", "../../web", "dev")
    .WithNpm(installCommand: "ci")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
