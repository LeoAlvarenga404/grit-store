var builder = DistributedApplication.CreateBuilder(args);

// Tags already in the local Docker cache (shared with the integration tests), to spare the Docker disk budget.
var database = builder.AddPostgres("postgres")
    .WithImageTag("18.6-alpine")
    .AddDatabase("grit");

// Declared now, referenced by the API when Basket (Redis) and product images (S3) arrive.
builder.AddRedis("redis").WithImageTag("8.10.2-alpine");
builder.AddSeaweedFS("storage").WithS3();

var api = builder.AddProject<Projects.Grit_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health");

builder.AddJavaScriptApp("web", "../../web", "dev")
    .WithNpm(installCommand: "ci")
    .WithHttpEndpoint(env: "PORT")
    .WithEnvironment("API_URL", api.GetEndpoint("http"))
    .WaitFor(api);

builder.Build().Run();
