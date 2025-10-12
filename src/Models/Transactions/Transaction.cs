using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PerInvest_API.src.Models.Transactions;

public class Transaction : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("date")]
    public DateTime Date { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("idCripto")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string IdCripto { get; set; } = string.Empty;

    [BsonElement("value")]
    public decimal Value { get; set; }

    [BsonElement("quotation")]
    public decimal Quotation { get; set; }

    [BsonElement("tax")]
    public decimal Tax { get; set; }

    [BsonElement("sold")]
    public bool Sold { get; set; }
}