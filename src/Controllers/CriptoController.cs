using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using PerInvest_Api.src.Data;

namespace PerInvest_API.src.Controllers;

public class CriptoController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetCripto).Produces<dynamic>();
        app.MapPost("/", AddCripto).Produces<dynamic>();
    }

    public static async Task<dynamic> GetCripto(AppDbContext context)
    {
        try
        {
            List<BsonDocument> result = await context.Criptos.Find(x => true).ToListAsync();
            List<dynamic> response = result.Select(x => BsonSerializer.Deserialize<dynamic>(x)).ToList();
            return new { success = true, data = response };
        }
        catch (Exception ex)
        {
            return new { success = false, message = $"Falha ao obter Criptos: {ex.Message}" };
        }
    }

    public static async Task<dynamic> AddCripto(AppDbContext context, dynamic body)
    {
        try
        {
            BsonDocument cripto = BsonDocument.Parse(body.GetRawText());
            await context.Criptos.InsertOneAsync(cripto);

            return new { success = true };
        }
        catch(Exception ex)
        {
            return new { success = false, message = $"Falha ao salvar Cripto: {ex.Message}" };
        }
    }
}