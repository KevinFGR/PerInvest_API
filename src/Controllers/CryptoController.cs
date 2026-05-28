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
using MongoDB.Bson;
using System.Globalization;
using PerInvest_API.src.Models.Transactions;
using MongoDB.Bson.Serialization;

namespace PerInvest_API.src.Controllers;

public class CryptoController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Get).RequireAuthorization();
        app.MapGet("/select", GetSelect).RequireAuthorization();
        app.MapGet("/{id}", GetById).RequireAuthorization();
        app.MapGet("/price-history/{id}", GetPriceHistory).RequireAuthorization();
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

    public static async Task<IResult> GetSelect(AppDbContext context, HttpContext httpContext)
    {
        try
        {
            Pagination<Crypto> pagination = new(httpContext);
            var data = await context.Cryptos
                .Find(pagination.Filter)
                .Sort(pagination.Sort)
                .Project(x => new {
                    x.Id,
                    x.Description
                })
                .ToListAsync();

            return new Response(data).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter Cryptos: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> GetById(AppDbContext context, HttpContext httpContext, IHttpClientFactory httpClientFactory,string id)
    {
        try
        {
            Pagination<Transaction> pagination = new(httpContext);
            BsonDocument[] pipeline = [
                new ("$match", new BsonDocument{
                    {"_id", new ObjectId(id)},
                    {"deleted", false}
                }),

                new ("$lookup", new BsonDocument{
                    {"from", "transactions"},
                    {"let", new BsonDocument("field0", "$_id")},
                    {"pipeline", new BsonArray{
                        new BsonDocument("$match", new BsonDocument( "$expr", new BsonDocument("$and", new BsonArray{
                            new BsonDocument("$eq", new BsonArray{"$$field0", "$idCrypto" }),
                            new BsonDocument("$eq", new BsonArray{"$deleted", false})
                        }))),

                        pagination.BsonFilter,
                        pagination.BsonSort,
                        pagination.BsonSkip,
                        pagination.BsonLimit,

                        new BsonDocument("$project", new BsonDocument{
                            {"_id", 0},
                            {"id", MongoHelper.ToString("$_id")},
                            {"value", 1},
                            {"quotation", 1},
                            {"tax", 1},
                            {"bank", 1},
                            {"type", 1},
                            {"date", 1},
                            {"sold", 1},
                        })
                    }},
                    {"as", "transactions"}
                }),

                new ("$project", new BsonDocument{
                    {"_id", 0},
                    {"id", MongoHelper.ToString("$_id")},
                    {"description", 1},
                    {"color", 1},
                    {"apiIndex", 1},
                    {"transactions", 1},
                    {"quotation", (decimal?)null},
                })
            ];
            dynamic data = await context.Cryptos.Aggregate<dynamic>(pipeline).FirstOrDefaultAsync();

            if(data is null)
                return new Response(400, "Crypto não encontrada").Result;

            Response apiResult = await HelperHttp.GetString(httpClientFactory, $"https://api.coingecko.com/api/v3/simple/price?ids={data.apiIndex}&vs_currencies=usd,brl");
            if (apiResult.Success)
            {
                BsonDocument bsonResult = BsonDocument.Parse(apiResult.Data);
                if (bsonResult.Contains(data.apiIndex))
                    data.quotation = Convert.ToDouble(bsonResult[data.apiIndex]["brl"].ToString(), CultureInfo.InvariantCulture);

                List<dynamic> transactions = data.transactions;
                data.transactions = transactions.Select(transaction =>
                {
                    if(transaction.type != "purchase" || transaction.sold )
                        return transaction;
                    
                    double? valorization = (data.quotation / transaction.quotation) - 1;
                    double? quotationNow = data.quotation;
                    double? valueNow = transaction.value + (transaction.value * valorization);

                    return new
                    {
                        transaction.date,
                        transaction.type,
                        transaction.value,
                        transaction.quotation,
                        transaction.tax,
                        transaction.bank,
                        quotationNow,
                        valorization = valorization == null ? null : valorization*100,
                        valueNow,
                    };
                }).ToList();
            }

            long count = await context.Transactions.CountDocumentsAsync(pagination.Filter & Builders<Transaction>.Filter.Eq(x => x.IdCrypto, id));

            return new PagedResponse<Transaction>(pagination, data, count).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter Crypto: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> GetPriceHistory(AppDbContext context, HttpContext httpContext, IHttpClientFactory httpClientFactory, string id)
    {
        try
        {
            Pagination<dynamic> pagination = new(httpContext);
            string? apiIndex = await context.Cryptos.Find(x => !x.Deleted && x.Id == id).Project(x => x.ApiIndex).FirstOrDefaultAsync();
            if(string.IsNullOrEmpty(apiIndex))
                return new Response(new { prices = new List<object>(), totalVolumes = new List<object>()}).Result;

            string uri = $"https://api.coingecko.com/api/v3/coins/{apiIndex}/market_chart/range?vs_currency=brl";
            if(pagination.BsonFilter["$match"].AsBsonDocument.Contains("from"))
                uri += $"&from={ConvertToUnixTimeSeconds(pagination.BsonFilter["$match"]["from"])}";
            if(pagination.BsonFilter["$match"].AsBsonDocument.Contains("to"))
                uri += $"&to={ConvertToUnixTimeSeconds(pagination.BsonFilter["$match"]["to"])}";
            if(pagination.BsonFilter["$match"].AsBsonDocument.Contains("days"))
                uri = $"https://api.coingecko.com/api/v3/coins/{apiIndex}/market_chart?vs_currency=brl&days={pagination.BsonFilter["$match"]["days"]}";
            Response apiResult = await HelperHttp.GetString(httpClientFactory, uri);
            if(!apiResult.Success)
                return new Response(new { prices = new List<object>(), totalVolumes = new List<object>()}).Result;

            BsonDocument bsonResult = BsonDocument.Parse(apiResult!.Data);
            List<object> prices = BsonSerializer.Deserialize<List<object>>(bsonResult["prices"].ToJson());
            List<object> volumes = BsonSerializer.Deserialize<List<object>>(bsonResult["total_volumes"].ToJson());

            return new Response(new{prices, volumes}).Result;
        }
        catch (Exception ex)
        {
            return new Response(400, $"Falha ao obter histórico da moeda: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Add(AppDbContext context, CreateCryptoDto request)
    {
        try
        {
            string? descriptionRepeated = await context.Cryptos.Find(x => 
                    x.Description.ToLower().Equals(request.Description.ToLower()) && !x.Deleted
                )
                .Project(x => x.Id)
                .FirstOrDefaultAsync();

            if(descriptionRepeated is not null) 
                return new Response(400, "Já existe uma Crypto cadastrada com essa descrição").Result;

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

            string? descriptionRepeated = await context.Cryptos.Find(x => 
                    x.Id != crypto.Id &&
                    x.Description.ToLower().Equals(request.Description.ToLower()) &&
                    !x.Deleted
                )
                .Project(x => x.Id)
                .FirstOrDefaultAsync();
            
            if(descriptionRepeated is not null) 
                return new Response(400, "Já existe uma Crypto cadastrada com essa descrição").Result;
            
            crypto.Description = request.Description;
            crypto.Color = request.Color;
            crypto.UpdatedBy = request.UserId;
            crypto.ApiIndex = request.ApiIndex;
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

    private static long ConvertToUnixTimeSeconds(BsonValue value)
    {
        return new DateTimeOffset(
            DateTime.Parse(value.AsString)
        ).ToUnixTimeSeconds();
    }
}