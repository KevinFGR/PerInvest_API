using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_API.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_API.src.Models.Criptos;
using System.Linq.Expressions;
using PerInvest_API.src.Dtos.Shared;
using PerInvest_API.src.Helpers;
using PerInvest_API.src.Common;
using MongoDB.Driver.Linq;

namespace PerInvest_API.src.Controllers;

public class CriptoController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Get);
        app.MapPost("/", Add).WithDataAnnotation<CreateCriptoDto>();
        app.MapPut("/", Update).WithDataAnnotation<UpdateCriptoDto>();
        app.MapDelete("/{id}", Delete);
    }

    public static async Task<IResult> Get(AppDbContext context, HttpContext httpContext)
    {
        try
        {
            Pagination<Cripto> pagination = new(httpContext);
            List<Cripto> data = await context.Criptos
                .Find(pagination.Filter)
                .Sort(pagination.Sort)
                .Skip(pagination.Skip)
                .Limit(pagination.Limit)
                .ToListAsync();
            return new Response(data).Result;
        }
        catch (Exception ex)
        {
            return new Response($"Falha ao obter Criptos: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Add(AppDbContext context, CreateCriptoDto request)
    {
        try
        {
            Cripto cripto = request.MapV2<Cripto>();
            cripto.CreatedAt = DateTime.Now;
            cripto.UpdatedAt = DateTime.Now;
            cripto.CreatedBy = request.UserId;
            cripto.UpdatedBy = request.UserId;

            await context.Criptos.InsertOneAsync(cripto);

            return new Response(201, "Cripto criada com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar Cripto: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Update(AppDbContext context, UpdateCriptoDto request)
    {
        try
        {
            Cripto? cripto = await context.Criptos.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
            if(cripto is null) return new Response("Cripto não encontrada").Result;
            
            cripto.Description = request.Description;
            cripto.Color = request.Color;
            cripto.UpdatedBy = request.UserId;
            cripto.UpdatedAt = DateTime.Now;

            Expression<Func<Cripto, bool>> filter = x => x.Id == request.Id && !x.Deleted;
            await context.Criptos.ReplaceOneAsync(filter, cripto);

            return new Response(200, "Cripto Atualizada com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar Cripto: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Delete(AppDbContext context, HttpContext httpContext, string id)
    {
        try
        {
            bool hasTransactions = await context.Transactions.AsQueryable().AnyAsync(x => !x.Deleted && x.IdCripto == id);
            if(hasTransactions) return new Response("Impossível excluir cripto, há movimentações cadastradas com esta moeda").Result;

            DeleteRequest request = new (httpContext, id);
            Expression<Func<Cripto, bool>> filter = x => x.Id == id;
            var update = Builders<Cripto>.Update
                .Set(x => x.Deleted, true)
                .Set(x => x.DeletedAt, DateTime.Now)
                .Set(x => x.DeletedBy, request.UserId);
            await context.Criptos.UpdateOneAsync(filter, update);

            return new Response(200, "Cripto Excluida com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar Cripto: {ex.Message}").Result;
        }
    }
}