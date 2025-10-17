using System.ComponentModel.DataAnnotations;
using PerInvest_API.src.Controllers;

namespace PerInvest_API.src.Common;
public static class Endpoint
{
    public static void MapEndpoints(this WebApplication app)
    {
        RouteGroupBuilder endpoints = app.MapGroup("");

        endpoints.MapGroup("/").WithTags("Health Check").MapGet("/", () => new { success = true });
        endpoints.MapGroup("api/auth").WithTags("Auth").MapEndpoint<AuthController>();
        endpoints.MapGroup("api/cryptos").WithTags("Cryptos").MapEndpoint<CryptoController>();
        endpoints.MapGroup("api/ip").WithTags("Ip").MapEndpoint<IpController>();
        endpoints.MapGroup("api/transactions").WithTags("Transactions").MapEndpoint<TransactionController>();
        endpoints.MapGroup("api/users").WithTags("Users").MapEndpoint<UserController>();
    }

    public static RouteHandlerBuilder WithDataAnnotation<TDto>(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            TDto? dto = context.Arguments.OfType<TDto>().FirstOrDefault();
            if (dto != null)
            {
                List<ValidationResult> results = [];
                ValidationContext validationContext = new (dto);
                bool isValid = Validator.TryValidateObject(dto, validationContext, results, true);

                // if (!isValid) return Results.BadRequest(new { Errors = results.Select(r => r.ErrorMessage) });
                if (!isValid)
                {
                    string combinedMessage = string.Join(" \n", results.Select(r => r.ErrorMessage));

                    return Results.BadRequest( new
                    {
                        success = false,
                        message = combinedMessage,
                        data = (object?)null
                    });
                }
            }

            return await next(context);
        });
    }

    private static IEndpointRouteBuilder MapEndpoint<TEndpoint>(this IEndpointRouteBuilder app)
    where TEndpoint : IEndpoint
    {
        TEndpoint.Map(app);
        return app;
    }
}