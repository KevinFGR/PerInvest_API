using PerInvest_API.src.Controllers;

namespace PerInvest_API.src.Common;
public static class Endpoint
{
    public static void MapEndpoints(this WebApplication app)
    {
        RouteGroupBuilder endpoints = app.MapGroup("");

        endpoints.MapGroup("/").WithTags("Health Check").MapGet("/", () => new { success = true });
        endpoints.MapGroup("api/v1/criptos").WithTags("Criptos").MapEndpoint<CriptoController>();
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
    where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}