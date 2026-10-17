using System.Text.Json.Serialization;
using FluentValidation;
using Grit.Api;
using Grit.BuildingBlocks.Behaviors;
using Grit.ServiceDefaults;
using MassTransit;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var modules = ModuleCatalog.Load(builder.Configuration);
var moduleAssemblies = modules.Select(module => module.GetType().Assembly).ToArray();

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("grit");
builder.Services.AddProblemDetails();
// The web defaults also read numbers from strings, which types every number as integer-or-string in OpenAPI and the web client.
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict);
builder.Services.AddOpenApi(OpenApiDocumentation.Configure);
builder.Services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);

// Only a known proxy (the Next.js BFF) may name the client of a request; ASP.NET Core already trusts loopback (ADR 0014).
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor;
    foreach (var network in builder.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
    {
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
});

builder.Services.AddValidatorsFromAssemblies(moduleAssemblies, includeInternalTypes: true);
builder.Services.AddMediatR(mediatr =>
{
    mediatr.RegisterServicesFromAssemblies(moduleAssemblies);
    mediatr.AddOpenBehavior(typeof(LoggingBehavior<,>));
    mediatr.AddOpenBehavior(typeof(ValidationBehavior<,>));
    mediatr.AddOpenBehavior(typeof(TransactionBehavior<,>));
});
builder.Services.AddMassTransit(bus =>
{
    foreach (var module in modules)
    {
        module.AddMessaging(bus);
    }

    bus.UsingInMemory((context, transport) => transport.ConfigureEndpoints(context));
});

foreach (var module in modules)
{
    module.AddServices(builder);
}

var app = builder.Build();

// Development covers the AppHost and the integration tests; the build-time OpenAPI run is Production and has no database.
if (app.Environment.IsDevelopment())
{
    foreach (var module in modules)
    {
        await module.MigrateDatabaseAsync(app.Services, app.Lifetime.ApplicationStopping);
    }
}

app.UseForwardedHeaders();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

var api = app.MapGroup("/api/v1");
foreach (var module in modules)
{
    module.MapEndpoints(api);
}

app.Run();
