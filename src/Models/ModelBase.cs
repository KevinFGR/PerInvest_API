using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PerInvest_API.src.Models;

public class ModelBase
{
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("createdBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CreatedBy { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [BsonElement("updatedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? UpdatedBy { get; set; }

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }

    [BsonElement("deletedBy")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? DeletedBy { get; set; }

    [BsonElement("deleted")]
    public bool Deleted { get; set; }
}