using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PerInvest_API.src.Models.Users;

public class User : ModelBase
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("salt")]
    public string Salt { get; set; } = string.Empty;

    [BsonElement("hash")]
    public string Hash { get; set; } = string.Empty;
}