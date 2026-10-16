using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Grit.Modules.Probe.Features.Fail;

internal static class FailEndpoint
{
    public static void MapFail(this IEndpointRouteBuilder probe) =>
        probe.MapGet("/fail", ThrowBug).WithSummary("Throws, to prove that a bug becomes a 500 ProblemDetails.");

    private static string ThrowBug() => throw new InvalidOperationException("Probe failure with a secret detail.");
}
