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
using PerInvest_API.src.Models.Cryptos;
using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper.Configuration;
using CsvHelper;

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
        app.MapPost("/import", ImportTransactions).DisableAntiforgery().RequireAuthorization();
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

    public static async Task<IResult> ImportTransactions(AppDbContext context, IFormFile csvFile)
    {
        try
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                return new Response(400, "Envie o arquivo").Result;
            }

            List<Transaction> transactions = [];
            using var stream = csvFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true
            };
            using var csv = new CsvReader(reader, config);
            csv.Read();
            csv.ReadHeader();
            while(csv.Read())
            {
                if(string.IsNullOrEmpty(csv.GetField("MOEDA")))
                    continue;

                string? idCrypto = await context.Cryptos
                    .Find(x => !x.Deleted && x.Description.ToUpper().Equals(csv.GetField("MOEDA")!.ToUpper()))
                    .Project(x => x.Id)
                    .FirstOrDefaultAsync();

                if(idCrypto is null)
                {
                    Crypto crypto = new()
                    {
                        Description = csv.GetField("MOEDA")!,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    await context.Cryptos.InsertOneAsync(crypto);
                    idCrypto = crypto.Id;
                }

                Transaction transaction = new();
                transaction.IdCrypto = idCrypto;
                transaction.Type = csv.GetField("TIPO")!.ToUpper().Equals("COMPRA") ? "PURCHASE" : "SALE";
                transaction.Date = DateTime.ParseExact(csv.GetField("DATA")!, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                transaction.Value = NormalizeCurrencyDouble(csv.GetField("VALOR")!);
                transaction.Tax = string.IsNullOrEmpty(csv.GetField("IMPOSTO")!) ? 0 : NormalizeCurrencyDouble(csv.GetField("IMPOSTO")!);
                transaction.Quotation = NormalizeCurrencyDouble(csv.GetField("COTAÇÃO")!);
                transaction.Sold = csv.GetField("TIPO")!.ToUpper().Equals("VENDA") || csv.GetField("VENDIDO")!.ToUpper().Equals("TRUE");
                transaction.Bank = csv.GetField("CORRETORA")!.ToUpper();
                transaction.CreatedAt = DateTime.Now;
                transaction.UpdatedAt = DateTime.Now;

                string? repeatedTransaction = await context.Transactions.Find( x
                    => x.IdCrypto == transaction.IdCrypto 
                    && x.Bank == transaction.Bank
                    && x.Value == transaction.Value
                    && x.Quotation == transaction.Quotation
                    && x.Date.Date == transaction.Date.Date
                    && x.Sold == transaction.Sold
                    && x.Tax == transaction.Tax
                    && x.Type == transaction.Type
                )
                .Project(x => x.Id)
                .FirstOrDefaultAsync();

                if(repeatedTransaction is null)
                    transactions.Add(transaction);

            }

            // while (!reader.EndOfStream)
            // {    
            //     var values = reader.ReadLine().Split(',');
            //     if(string.IsNullOrEmpty(values[0]))
            //         continue;

            //     string? idCrypto = await context.Cryptos
            //         .Find(x => !x.Deleted && x.Description.ToUpper().Equals(values[0].ToUpper()))
            //         .Project(x => x.Id)
            //         .FirstOrDefaultAsync();
                
            //     if(idCrypto is null)
            //     {
            //         Crypto crypto = new()
            //         {
            //             Description = values[0],
            //             CreatedAt = DateTime.Now,
            //             UpdatedAt = DateTime.Now
            //         };

            //         await context.Cryptos.InsertOneAsync(crypto);
            //         idCrypto = crypto.Id;
            //     }

            //     Transaction transaction = new();
            //     transaction.IdCrypto = idCrypto;
            //     transaction.Type = values[1].ToUpper().Equals("COMPRA") ? "PURCHASE" : "SALE";
            //     transaction.Date = DateTime.ParseExact(values[2], "dd/MM/yyyy", CultureInfo.InvariantCulture);
            //     transaction.Value = NormalizeCurrencyDouble(values[3]);
            //     transaction.Tax = string.IsNullOrEmpty(values[4]) ? 0 : NormalizeCurrencyDouble(values[4]);
            //     transaction.Quotation = NormalizeCurrencyDouble(values[5]);
            //     transaction.Sold = values[1].ToUpper().Equals("VENDA") || values[8].ToUpper().Equals("TRUE");
            //     transaction.Bank = values[9].ToUpper();
            //     transaction.CreatedAt = DateTime.Now;
            //     transaction.UpdatedAt = DateTime.Now;
            //     // {
            //     //     IdCrypto = idCrypto,
            //     //     Type = values[1].ToUpper().Equals("COMPRA") ? "PURCHASE" : "SALE",
            //     //     Date = DateTime.ParseExact(values[2], "dd/MM/yyyy", CultureInfo.InvariantCulture),
            //     //     Value = NormalizeCurrencyDouble(values[3]),
            //     //     Tax = string.IsNullOrEmpty(values[4]) ? 0 : NormalizeCurrencyDouble(values[4]),
            //     //     Quotation = NormalizeCurrencyDouble(values[5]),
            //     //     Sold = values[1].ToUpper().Equals("VENDA") || values[8].ToUpper().Equals("TRUE"),
            //     //     Bank = values[9].ToUpper(),
            //     //     CreatedAt = DateTime.Now,
            //     //     UpdatedAt = DateTime.Now,
            //     // };

            //     string? repeatedTransaction = await context.Transactions.Find( x
            //             => x.IdCrypto == transaction.IdCrypto 
            //             && x.Bank == transaction.Bank
            //             && x.Value == transaction.Value
            //             && x.Quotation == transaction.Quotation
            //             && x.Date.Date == transaction.Date.Date
            //             && x.Sold == transaction.Sold
            //             && x.Tax == transaction.Tax
            //             && x.Type == transaction.Type
            //         )
            //         .Project(x => x.Id)
            //         .FirstOrDefaultAsync();

            //     if(repeatedTransaction is null)
            //         transactions.Add(transaction);
            // }

            await context.Transactions.InsertManyAsync(transactions);

            return new Response(201, "Movimentações inseridas com sucesso").Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao importar movimentações: {ex.Message}").Result;
        }
    }

    private static double NormalizeCurrencyDouble(string value)
    {
        string justNumbers = Regex.Replace(value, @"[^\d,\.]", "");
        return double.Parse(justNumbers, new CultureInfo("pt-BR"));
    }
}