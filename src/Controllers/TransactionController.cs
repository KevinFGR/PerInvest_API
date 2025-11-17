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
        app.MapGet("/", Get).RequireAuthorization();
        app.MapGet("/{id}", GetById).RequireAuthorization();
        app.MapPost("/", Add).RequireAuthorization().WithDataAnnotation<CreateTransactionDto>();
        app.MapPut("/", Update).RequireAuthorization().WithDataAnnotation<UpdateTransactionDto>();
        app.MapDelete("/{id}", Delete).RequireAuthorization();
    }

    public static async Task<IResult> Get(AppDbContext context, HttpContext httpContext)
    {
        try
        {
            Pagination<Transaction> pagination = new(httpContext);

            BsonDocument[] pipeline = [
                pagination.BsonFilter,
                pagination.BsonSort,
                pagination.BsonSkip,
                pagination.BsonLimit, 
                new("$lookup", new BsonDocument{
                    {"from", "cryptos"},
                    {"let", new BsonDocument("idCrypto", "$idCrypto")},
                    {"pipeline", new BsonArray{
                        new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray{"$_id", "$$idCrypto"})))
                    }},
                    {"as", "descriptionCrypto"}
                }),

                new ("$addFields", new BsonDocument{
                    {"id", new BsonDocument("$toString", "$_id")},
                    {"idCrypto", new BsonDocument("$toString", "$idCrypto")},
                    {"descriptionCrypto", new BsonDocument("$first", "$descriptionCrypto.description")}
                }),

                new ("$project", new BsonDocument("_id", 0))

            ];

            pipeline.ToList().ForEach(x => { System.Console.WriteLine(x);  System.Console.WriteLine(",");});

            List<BsonDocument> bsonData = await context.Transactions.Aggregate<BsonDocument>(pipeline).ToListAsync();
            long count = await context.Transactions.CountDocumentsAsync(pagination.Filter);

            return new PagedResponse<Transaction>(pagination, bsonData.ToDynamic(), count).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter movimentações: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> GetById(AppDbContext context, HttpContext httpContext, string id)
    {
        try
        {
            Transaction data = await context.Transactions
                .Find(x=> x.Id ==id && !x.Deleted)
                .FirstOrDefaultAsync();

            return new Response(data).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter movimentação: {ex.Message}").Result;
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
            return new Response(500, $"Falha ao salvar movimentação: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Update(AppDbContext context, UpdateTransactionDto request)
    {
        try
        {
            Transaction? dbTransaction = await context.Transactions.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
            if(dbTransaction is null) return new Response(400, "Movimentação não encontrada").Result;

            bool cryptoExists = await context.Cryptos.AsQueryable().AnyAsync(x => x.Id == request.IdCrypto && !x.Deleted);
            if(!cryptoExists) return new Response(400, "Crypto não encontrada").Result;

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
            return new Response(500, $"Falha ao atualizar movimentação: {ex.Message}").Result;
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
            return new Response(500, $"Falha ao excluir movimentação: {ex.Message}").Result;
        }
    }
}