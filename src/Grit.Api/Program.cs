using FluentValidation;
using Grit.Api;
using Grit.BuildingBlocks.Behaviors;
using Grit.ServiceDefaults;
using MassTransit;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var modules = ModuleCatalog.Load(builder.Configuration);
var moduleAssemblies = modules.Select(module => module.GetType().Assembly).ToArray();

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("grit");
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddRateLimiter(options => options.RejectionStatusCode = StatusCodes.Status429TooManyRequests);
builder.Services.AddValidatorsFromAssemblies(moduleAssemblies, includeInternalTypes: true);
builder.Services.AddMediatR(mediatr =>
{
    // MediatR rejects an empty assembly list, and the API alone has no module yet (the probe lives in the tests).
    mediatr.RegisterServicesFromAssemblies([typeof(Program).Assembly, .. moduleAssemblies]);
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

app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
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
