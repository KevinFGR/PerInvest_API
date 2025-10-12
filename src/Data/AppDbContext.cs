using MongoDB.Driver;
using PerInvest_Api.src.Models.Criptos;
using PerInvest_Api.src.Models.Transactions;

namespace PerInvest_Api.src.Data;

public class AppDbContext
{
    public static string? ConnectionString { get; set; }
    public static string? DatabaseName { get; set; }
    public static bool IsSSL { get; set; }
    private IMongoDatabase _database { get; }

    public AppDbContext()
    {
        try
        {
            MongoClientSettings mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(ConnectionString));
            if (IsSSL)
            {
                mongoClientSettings.SslSettings = new SslSettings
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12
                };
            }

            var mongoClient = new MongoClient(mongoClientSettings);
            _database = mongoClient.GetDatabase(DatabaseName);
        }
        catch
        {
            throw new Exception("Falha ao conectar com banco de dados");
        }
    }

    public IMongoCollection<Cripto> Criptos
    {
        get { return _database.GetCollection<Cripto>("mdt_criptos"); }
    }

    public IMongoCollection<Transaction> Transactions
    {
        get { return _database.GetCollection<Transaction>("mdt_transactions"); }
    }

}