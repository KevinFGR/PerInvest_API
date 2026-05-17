using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_API.src.Data;
using PerInvest_API.src.Helpers;
using MongoDB.Driver.Linq;
using MongoDB.Bson;
using System.Globalization;

namespace PerInvest_API.src.Controllers;

public class HomeController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Home).RequireAuthorization();
    }

    public static async Task<IResult> Home(AppDbContext context, HttpContext httpContext, IHttpClientFactory httpClientFactory)
    {
        try
        {
            List<dynamic> cryptosPrice = await GetAllCryptoPrice(context, httpClientFactory);
            List<dynamic> cheepestOperation = await GetCheepestPurchase(context);

            cheepestOperation = cheepestOperation.Select(x => {
                dynamic? currentCrypto = cryptosPrice.Where(y => y.apiIndex == x.apiIndex).FirstOrDefault();
                double? valorization = currentCrypto == null ? null : (currentCrypto.value / x.quotation) - 1;
                double? quotationNow = currentCrypto?.value ?? null;
                double? valueNow = currentCrypto == null ? null : x.value + (x.value * valorization);

                return new
                {
                    x.value,
                    x.description,
                    x.quotation,
                    quotationNow,
                    valorization = valorization == null ? null : valorization*100,
                    valueNow,
                    x.color
                } as dynamic;
            }).ToList();

            cryptosPrice = cryptosPrice.Select(x => new { x.description, x.value, x.color } as dynamic).ToList();

            return new Response(new {
                cryptosPrice,
                cheepestOperation
            }).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter Cryptos: {ex.Message}").Result;
        }
    }

    public static async Task<List<dynamic>> GetCheepestPurchase(AppDbContext context)
    {
        BsonDocument[] pipeline = [
            new ("$match", new BsonDocument{
                {"deleted", false},
                {"type", "purchase"},
                {"sold", false},
            }),

            new("$sort", new BsonDocument("quotation", 1)),
            new("$group", new BsonDocument
            {
                { "_id", "$idCrypto" },
                { "document", new BsonDocument("$first", "$$ROOT") }
            }),

            new("$replaceRoot", new BsonDocument
            {
                { "newRoot", "$document" }
            }),

            MongoHelper.Lookup("cryptos", "crypto", [
                ["$idCrypto", "$_id"],
                [false, "$deleted"],
            ] ,1),

            new ("$project", new BsonDocument {
                {"_id", 0},
                // {"id", new BsonDocument("$toString", "$_id")},
                {"value", 1},
                {"quotation", 1},
                {"description", new BsonDocument("$first", "$crypto.description")},
                {"apiIndex", new BsonDocument("$first", "$crypto.apiIndex")},
                {"color", new BsonDocument("$first", "$crypto.color")}
            }),

            new("$sort", new BsonDocument("description", 1)),
        ];

        var data = await context.Transactions.Aggregate<BsonDocument>(pipeline).ToListAsync();
        return data.ToDynamic();
    }

    public static async Task<List<dynamic>> GetAllCryptoPrice(AppDbContext context, IHttpClientFactory httpClientFactory)
    {
        var cryptos = await context.Cryptos
            .Find(x => !x.Deleted && !string.IsNullOrEmpty(x.ApiIndex))
            .Project(x => new{
                x.ApiIndex,
                x.Description,
                x.Color
            })
            .ToListAsync();

        string cryptosForUrn = string.Join(",", cryptos.Select(x => x.ApiIndex));

        Response apiResult = await HelperHttp.GetString(httpClientFactory, $"https://api.coingecko.com/api/v3/simple/price?ids={cryptosForUrn}&vs_currencies=usd,brl");
        if(!apiResult.Success)
            return [];

        BsonDocument bsonResult = BsonDocument.Parse(apiResult.Data);
        List<dynamic> response = bsonResult.Elements.Select(prop => new{
            description = cryptos.Where(x => x.ApiIndex == prop.Name).Select(x => x.Description).FirstOrDefault(),
            apiIndex = prop.Name,
            value = Convert.ToDouble(prop.Value["brl"].ToString(), CultureInfo.InvariantCulture),
            color = cryptos.Where(x => x.ApiIndex == prop.Name).Select(x => x.Color).FirstOrDefault()
        } as dynamic).ToList();

        return response;
    }

}