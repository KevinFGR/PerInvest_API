using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PerInvest_API.src.Models.Cryptos;

public class Crypto : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("color")]
    public string Color { get; set; } = string.Empty;
}