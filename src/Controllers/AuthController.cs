using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_API.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_API.src.Models.Users;
using PerInvest_API.src.Helpers;
using MongoDB.Driver.Linq;

namespace PerInvest_API.src.Controllers;

public class AuthController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/", Auth);
    }


    public static async Task<IResult> Auth(AppDbContext context, IConfiguration configuration, AuthDto request)
    {
        try
        {
            User? user = await context.Users.Find(x => x.Email == request.Email && !x.Deleted).FirstOrDefaultAsync();
            if(user is null) 
                return new Response(400, "Credenciais inválidas").Result;

            if(!HelperPerInvest.PasswordIsValid(request.Password, user.Salt, user.Hash))
                return new Response(400, "Credenciais inválidas").Result;

            dynamic response = new
            {
                token = HelperPerInvest.GenerateToken(user, configuration),
            };

            return new Response(response).Result;
        }
        catch(Exception ex)
        {
            return new Response(500, $"Falha ao salvar usuário: {ex.Message}").Result;
        }
    }
}