using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_API.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_API.src.Models.Cryptos;
using System.Linq.Expressions;
using PerInvest_API.src.Dtos.Shared;
using PerInvest_API.src.Helpers;
using PerInvest_API.src.Common;
using MongoDB.Driver.Linq;

namespace PerInvest_API.src.Controllers;

public class CryptoController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Get).RequireAuthorization();
        app.MapPost("/", Add).RequireAuthorization().WithDataAnnotation<CreateCryptoDto>();
        app.MapPut("/", Update).RequireAuthorization().WithDataAnnotation<UpdateCryptoDto>();
        app.MapDelete("/{id}", Delete).RequireAuthorization();
    }

    public static async Task<IResult> Get(AppDbContext context, HttpContext httpContext)
    {
        try
        {
            Pagination<Crypto> pagination = new(httpContext);
            List<Crypto> data = await context.Cryptos
                .Find(pagination.Filter)
                .Sort(pagination.Sort)
                .Skip(pagination.Skip)
                .Limit(pagination.Limit)
                .ToListAsync();
            long count = await context.Cryptos.CountDocumentsAsync(pagination.Filter);

            return new PagedResponse<Crypto>(pagination, data, count).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter Cryptos: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Add(AppDbContext context, CreateCryptoDto request)
    {
        try
        {
            Crypto crypto = request.Map<Crypto>();
            crypto.CreatedAt = DateTime.Now;
            crypto.UpdatedAt = DateTime.Now;
            crypto.CreatedBy = request.UserId;
            crypto.UpdatedBy = request.UserId;

            await context.Cryptos.InsertOneAsync(crypto);

            return new Response(201, "Crypto criada com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response(500, $"Falha ao salvar Crypto: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Update(AppDbContext context, UpdateCryptoDto request)
    {
        try
        {
            Crypto? crypto = await context.Cryptos.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
            if(crypto is null) return new Response(400, "Crypto não encontrada").Result;
            
            crypto.Description = request.Description;
            crypto.Color = request.Color;
            crypto.UpdatedBy = request.UserId;
            crypto.UpdatedAt = DateTime.Now;

            Expression<Func<Crypto, bool>> filter = x => x.Id == request.Id && !x.Deleted;
            await context.Cryptos.ReplaceOneAsync(filter, crypto);

            return new Response(200, "Crypto Atualizada com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response(500, $"Falha ao atualizar Crypto: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Delete(AppDbContext context, HttpContext httpContext, string id)
    {
        try
        {
            bool hasTransactions = await context.Transactions.AsQueryable().AnyAsync(x => !x.Deleted && x.IdCrypto == id);
            if(hasTransactions) return new Response(400, "Impossível excluir crypto, há movimentações cadastradas com esta moeda").Result;

            DeleteRequest request = new (httpContext, id);
            Expression<Func<Crypto, bool>> filter = x => x.Id == id;
            var update = Builders<Crypto>.Update
                .Set(x => x.Deleted, true)
                .Set(x => x.DeletedAt, DateTime.Now)
                .Set(x => x.DeletedBy, request.UserId);
            await context.Cryptos.UpdateOneAsync(filter, update);

            return new Response(200, "Crypto Excluida com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response(500, $"Falha ao excluir crypto: {ex.Message}").Result;
        }
    }
}