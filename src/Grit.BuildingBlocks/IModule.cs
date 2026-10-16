using MassTransit;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;

namespace Grit.BuildingBlocks;

public interface IModule
{
    void AddServices(IHostApplicationBuilder builder);

    void AddMessaging(IBusRegistrationConfigurator bus);

    void MapEndpoints(IEndpointRouteBuilder api);
}
