using Grit.BuildingBlocks;
using Grit.Modules.Basket.Contracts;
using Grit.Modules.Basket.Features.AddBasketLine;
using Grit.Modules.Basket.Features.ChangeBasketLine;
using Grit.Modules.Basket.Features.GetBasket;
using Grit.Modules.Basket.Features.RemoveBasketLine;
using Grit.Modules.Basket.Infrastructure;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Grit.Modules.Basket;

public sealed class BasketModule : IModule
{
    public void AddServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Connects on first use: the build-time OpenAPI run composes the modules without configuration (ADR 0004).
        builder.Services.AddSingleton<IConnectionMultiplexer>(services => ConnectionMultiplexer.Connect(
            services.GetRequiredService<IConfiguration>().GetConnectionString("redis")
            ?? throw new InvalidOperationException("Connection string 'redis' is missing.")));
        builder.Services.AddSingleton<BasketStore>();
        builder.Services.AddScoped<IBasketReader, BasketReader>();
    }

    public void AddMessaging(IBusRegistrationConfigurator bus)
    {
        ArgumentNullException.ThrowIfNull(bus);

        bus.AddConsumer<ClearBasketConsumer>();
    }

    // The basket lives only in Redis.
    public Task MigrateDatabaseAsync(IServiceProvider services, CancellationToken cancellationToken) => Task.CompletedTask;

    public void MapEndpoints(IEndpointRouteBuilder api)
    {
        var basket = api.MapGroup("/basket").WithTags("Basket").RequireAuthorization();
        basket.MapGetBasket();
        basket.MapAddBasketLine();
        basket.MapChangeBasketLine();
        basket.MapRemoveBasketLine();
    }
}
