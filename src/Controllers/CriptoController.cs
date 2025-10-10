using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_Api.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_Api.src.Models.Criptos;

namespace PerInvest_API.src.Controllers;

public class CriptoController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetCripto).Produces<dynamic>();
        app.MapPost("/", AddCripto).Produces<dynamic>();
    }

    public static async Task<IResult> GetCripto(AppDbContext context)
    {
        try
        {
            List<Cripto> data = await context.Criptos.Find(x => true).ToListAsync();
            return new Response(data).Result;
        }
        catch (Exception ex)
        {
            return new Response($"Falha ao obter Criptos: {ex.Message}").Result;
        }
    }

    public static async Task<dynamic> AddCripto(AppDbContext context, CreateCriptoDto body)
    {
        try
        {
            Cripto cripto = new(){
                Description = body.Description,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await context.Criptos.InsertOneAsync(cripto);

            return new Response(200).Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar Cripto: {ex.Message}").Result;
        }
    }
}