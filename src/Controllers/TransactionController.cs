using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_Api.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_Api.src.Models.Transactions;

namespace PerInvest_API.src.Controllers;

public class TransactionController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Get).Produces<dynamic>();
        app.MapPost("/", Add).Produces<dynamic>();
    }

    public static async Task<IResult> Get(AppDbContext context)
    {
        try
        {
            List<Transaction> data = await context.Transactions.Find(x => true).ToListAsync();
            return new Response(data).Result;
        }
        catch (Exception ex)
        {
            return new Response($"Falha ao obter movimentações: {ex.Message}").Result;
        }
    }

    public static async Task<dynamic> Add(AppDbContext context, CreateTransactionDto body)
    {
        try
        {
            Transaction transaction = new(){
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await context.Transactions.InsertOneAsync(transaction);

            return new Response(200).Result;
        }
        catch(Exception ex)
        {
            return new Response($"Falha ao salvar movimentação: {ex.Message}").Result;
        }
    }
}