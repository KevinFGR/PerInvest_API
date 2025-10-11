using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_Api.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_Api.src.Models.Criptos;
using System.Linq.Expressions;
using PerInvest_API.src.Dtos.Shared;

namespace PerInvest_API.src.Controllers;

public class CriptoController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Get).Produces<dynamic>();
        app.MapPost("/", Add).Produces<dynamic>();
        app.MapPut("/", Update);
        app.MapDelete("/", Delete);
    }

    public static async Task<IResult> Get(AppDbContext context)
    {
        try
        {
            List<Cripto> data = await context.Criptos.Find(x => !x.Deleted).ToListAsync();
            return new Response(data).Result;
        }
        catch (Exception ex)
        {
            return new Response($"Falha ao obter Criptos: {ex.Message}").Result;
        }
    }

    public static async Task<dynamic> Add(AppDbContext context, CreateCriptoDto request)
    {
        try
        {
            Cripto cripto = new(){
                Description = request.Description,
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

    public static async Task<dynamic> Update(AppDbContext context, UpdateCriptoDto request)
    {
        try
        {
            Cripto cripto = await context.Criptos.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
            cripto.Description = request.Description;
            cripto.UpdatedBy = request.UserId;
            cripto.UpdatedAt = DateTime.Now;

            Expression<Func<Cripto, bool>> filter = x => x.Id == request.Id && !x.Deleted;
            await context.Criptos.ReplaceOneAsync(filter, cripto);

            return new Response(200).Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar Cripto: {ex.Message}").Result;
        }
    }

    public static async Task<dynamic> Delete(AppDbContext context, DeleteRequest request)
    {
        try
        {
            Expression<Func<Cripto, bool>> filter = x => x.Id == request.Id;
            var update = Builders<Cripto>.Update
                .Set(x => x.Deleted, true)
                .Set(x => x.DeletedAt, DateTime.Now)
                .Set(x => x.DeletedBy, request.UserId);
            await context.Criptos.UpdateOneAsync(filter, update);

            return new Response(200).Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar Cripto: {ex.Message}").Result;
        }
    }
}