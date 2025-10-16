using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_API.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_API.src.Models.Transactions;
using PerInvest_API.src.Helpers;
using MongoDB.Driver.Linq;
using PerInvest_API.src.Common;
using PerInvest_API.src.Dtos.Shared;
using System.Linq.Expressions;
using MongoDB.Bson;

namespace PerInvest_API.src.Controllers;

public class TransactionController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Get);
        app.MapPost("/", Add).WithDataAnnotation<CreateTransactionDto>();
        app.MapPut("/", Update).WithDataAnnotation<UpdateTransactionDto>();
        app.MapDelete("/{id}", Delete);
    }

    public static async Task<IResult> Get(AppDbContext context, HttpContext httpContext)
    {
        try
        {
            Pagination<Transaction> pagination = new(httpContext);

            BsonDocument[] pipeline = [
                pagination.Match,

                new("$lookup", new BsonDocument{
                    {"from", "criptos"},
                    {"let", new BsonDocument("idCripto", "$idCripto")},
                    {"pipeline", new BsonArray{
                        new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray{"$_id", "$$idCripto"})))
                    }},
                    {"as", "descriptionCrypto"}
                }),

                new ("$addFields", new BsonDocument{
                    {"id", new BsonDocument("$toString", "$_id")},
                    {"idCripto", new BsonDocument("$toString", "$idCripto")},
                    {"descriptionCrypto", new BsonDocument("$first", "$descriptionCrypto.description")}
                }),

                new ("$project", new BsonDocument("_id", 0))
            ];

            List<BsonDocument> bsonData = await context.Transactions.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return new Response(bsonData.ToDynamic()).Result;
        }
        catch (Exception ex)
        {
            return new Response($"Falha ao obter movimentações: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Add(AppDbContext context, CreateTransactionDto request)
    {
        try
        {
            Transaction transaction = request.Map<Transaction>();
            if(transaction.Type == "sale") transaction.Sold = true;
            transaction.CreatedAt = DateTime.Now;
            transaction.UpdatedAt = DateTime.Now;
            transaction.CreatedBy = request.UserId;
            transaction.UpdatedBy = request.UserId;

            await context.Transactions.InsertOneAsync(transaction);

            return new Response(200).Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar movimentação: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Update(AppDbContext context, UpdateTransactionDto request)
    {
        try
        {
            Transaction? dbTransaction = await context.Transactions.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
            if(dbTransaction is null) return new Response("Movimentação não encontrada").Result;

            bool criptoExists = await context.Criptos.AsQueryable().AnyAsync(x => x.Id == request.IdCripto && !x.Deleted);
            if(!criptoExists) return new Response("Cripto não encontrada").Result;

            Transaction transaction = request.Map<Transaction>();
            if(transaction.Type == "sale") transaction.Sold = true;
            transaction.CreatedAt = dbTransaction.CreatedAt;
            transaction.CreatedBy = dbTransaction.CreatedBy;
            transaction.UpdatedAt = DateTime.Now;
            transaction.UpdatedBy = request.UserId;

            Expression<Func<Transaction, bool>> filter = x => x.Id == request.Id && !x.Deleted;
            await context.Transactions.ReplaceOneAsync(filter, transaction);

            return new Response(200).Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar movimentação: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Delete(AppDbContext context, HttpContext httpContext, string id)
    {
        try
        {
            DeleteRequest request = new (httpContext, id);
            Expression<Func<Transaction, bool>> filter = x => x.Id == id;
            UpdateDefinition<Transaction> update = Builders<Transaction>.Update
                .Set(x => x.Deleted, true)
                .Set(x => x.DeletedAt, DateTime.Now)
                .Set(x => x.DeletedBy, request.UserId);
            await context.Transactions.UpdateOneAsync(filter, update);

            return new Response(200, "Movimentação Excluida com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar movimentação: {ex.Message}").Result;
        }
    }
}