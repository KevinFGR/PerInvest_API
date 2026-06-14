using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_API.src.Data;
using PerInvest_API.src.Helpers;
using MongoDB.Driver.Linq;
using MongoDB.Bson;
using System.Globalization;
using MongoDB.Bson.Serialization;

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
            List<dynamic> cheepestPurchase = await GetCheepestPurchase(context, cryptosPrice);
            List<dynamic> totalCryptoPurchased = await GetTotalCryptoPurchased(context, cheepestPurchase);

            return new Response(totalCryptoPurchased).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter Cryptos: {ex.Message}").Result;
        }
    }

    public static async Task<List<dynamic>> GetCheepestPurchase(AppDbContext context, List<dynamic> cryptosPrice)
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
                {"value", 1},
                {"quotation", 1},
                {"date", 1},
                {"bank", 1},
                {"apiIndex", new BsonDocument("$first", "$crypto.apiIndex")},
                {"idCrypto", MongoHelper.ToStringFirst("$crypto._id")},
            }),
        ];

        var data = await context.Transactions.Aggregate<BsonDocument>(pipeline).ToListAsync();
        List<dynamic> cheepestPurchase = data.ToDynamic();

        return cheepestPurchase.Select(x => {
            dynamic? currentCrypto = cryptosPrice.Where(y => y.idCrypto == x.idCrypto).FirstOrDefault();
            double? valorization = currentCrypto == null ? null : (currentCrypto.value / x.quotation) - 1;
            double? quotationNow = currentCrypto?.value ?? null;
            double? valueNow = currentCrypto == null ? null : x.value + (x.value * valorization);

            return new
            {
                x.idCrypto,
                x.value,
                x.quotation,
                quotationNow,
                valorization = valorization == null ? null : valorization*100,
                valueNow,
                x.date,
                x.bank,
            } as dynamic;
        }).ToList();
    }

    public static async Task<List<dynamic>> GetAllCryptoPrice(AppDbContext context, IHttpClientFactory httpClientFactory)
    {
        var cryptos = await context.Cryptos
            .Find(x => !x.Deleted && !string.IsNullOrEmpty(x.ApiIndex))
            .Project(x => new{
                x.Id,
                x.ApiIndex,
                x.ApiIndex2,
            })
            .ToListAsync();

        string cryptosForUrn = string.Join(",", cryptos.Select(x => x.ApiIndex));
        Response apiResult = await HelperHttp.GetString(httpClientFactory, $"https://api.coingecko.com/api/v3/simple/price?ids={cryptosForUrn}&vs_currencies=usd,brl");
        BsonDocument planB = [];
        if(!apiResult.Success)
        {
            Response apiResult2 = await HelperHttp.GetString(httpClientFactory, $"https://api.binance.com/api/v3/ticker/price");
            if(!apiResult2.Success)
                return [];

            BsonArray bsonResult2 = BsonSerializer.Deserialize<BsonArray>(apiResult2.Data);
            foreach(var crypto in cryptos)
            {
                BsonDocument? objectQuotation = bsonResult2.OfType<BsonDocument>()
                    .FirstOrDefault(x 
                        => x.Contains("symbol") 
                        && string.Equals( x["symbol"].AsString, crypto.ApiIndex2, StringComparison.OrdinalIgnoreCase));
                
                if(objectQuotation is not null)
                    planB[crypto.ApiIndex] = new BsonDocument("brl", objectQuotation["price"].AsString);
            }   
        }

        BsonDocument bsonResult = apiResult.Success ? BsonDocument.Parse(apiResult.Data) : planB;
        List<dynamic> response = bsonResult.Elements.Select(prop => {
            var currentCrypto = cryptos.Where(x => x.ApiIndex == prop.Name).FirstOrDefault();
            return new
            {
                idCrypto = currentCrypto?.Id ?? "",
                apiIndex = prop.Name,
                value = Convert.ToDouble(prop.Value["brl"].ToString(), CultureInfo.InvariantCulture),
            } as dynamic;
        }).ToList();

        return response;
    }

    public static async Task<List<dynamic>> GetTotalCryptoPurchased(AppDbContext context, List<dynamic> cheepestPurchases)
    {
        BsonDocument[] pipeline = [
            new ("$match", new BsonDocument{
                {"deleted", false},
                {"type", "purchase"},
                {"sold", false},
            }),
            new ("$group", new BsonDocument{
                {"_id", "$idCrypto"},
                {"transactions", new BsonDocument("$push", new BsonDocument{
                    {"value", new BsonDocument("$subtract", new BsonArray{"$value", "$tax"})},
                    {"quotation", "$quotation"}
                })}
            }),

            MongoHelper.Lookup("cryptos", "crypto", [
                ["$_id", "$_id"],
                [false, "$deleted"]
            ], 1),

            new ("$project", new BsonDocument{
                {"idCrypto", MongoHelper.ToString("$_id")},
                {"transactions", 1},
                {"description", MongoHelper.First("$crypto.description")},
                {"color", MongoHelper.First("$crypto.color")},
                {"_id", 0},
            })
        ];

        List<dynamic> result = await context.Transactions.Aggregate<dynamic>(pipeline).ToListAsync();

        return result.Select(x => {
            List<dynamic> transactions = x.transactions;
            dynamic? cheepestPurchase = cheepestPurchases.FirstOrDefault(y => y.idCrypto == x.idCrypto);
            double quotationNow = cheepestPurchase == null ? 0 : cheepestPurchase.quotationNow ?? 0;
            double totalNow = 0;
            double totalPurchased = 0;
            foreach(var transaction in transactions){
                totalNow += quotationNow * (transaction.value / transaction.quotation);
                totalPurchased += transaction.value;
            }
            return new { 
                x.idCrypto,
                x.description,
                x.color,
                totalPurchased,
                quotationNow,
                totalNow,
                valorization = quotationNow == 0 ? 0 : ((totalNow / totalPurchased) - 1) * 100,
                cheepestPurchase = cheepestPurchase == null ? null : new { 
                    cheepestPurchase.value,
                    cheepestPurchase.quotation,
                    cheepestPurchase.valorization,
                    cheepestPurchase.valueNow,
                    cheepestPurchase.date,
                    cheepestPurchase.bank,
                }
            } as dynamic;
        })
        .OrderByDescending(x => x.totalPurchased)
        .ToList();
    }
}