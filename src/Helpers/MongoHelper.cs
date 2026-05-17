using MongoDB.Bson;
using MongoDB.Driver;

namespace PerInvest_API.src.Helpers;

public static class MongoHelper{
    public static AggregateOptions AggregateOptions { get => new() { Collation = new Collation("pt", strength: CollationStrength.Secondary) }; }
    public static FindOptions FindOptions { get => new () { Collation = new Collation("pt", strength: CollationStrength.Secondary) }; }

    public static BsonDocument ToString(string value) => new ("$toString", value);
    public static BsonDocument ToStringFirst(string value) => new ("$toString", new BsonDocument("$first", value));
    public static BsonDocument Push(dynamic value) => new ("$push", value);
    public static BsonDocument First(dynamic value, int layers = 1)
    {
        BsonDocument bsonValue = new("$first", value);
        for (int i = 1; i < layers; i++)
        {
            bsonValue = new("$first", bsonValue);
        }

        return bsonValue;
    }
    public static BsonDocument Regex(BsonValue value) => new(){
        {"$regex", value},
        {"$options", "i"}
    };

    public static BsonDocument Unwind(string property) => new ("$unwind", new BsonDocument
    {
        { "path", property },
        { "preserveNullAndEmptyArrays", true }
    });

        // return new BsonDocument("$cond", new BsonArray{
        //     new BsonDocument("$ne", new BsonArray { property, BsonNull.Value }),
        //         orElse,
        //         then
        // });
    public static BsonDocument IfNull(dynamic property, dynamic? then, dynamic orElse)
    {
        // ESSA FUNÇÃO NÃO FAZ SENTIDO NENHUM
        return new BsonDocument("$cond", new BsonArray{
            new BsonDocument("$or", new BsonArray
            {
                // É null
                new BsonDocument("$eq", new BsonArray
                {
                    property,
                    BsonNull.Value
                }),

                // Campo não existe
                new BsonDocument("$eq", new BsonArray
                {
                    new BsonDocument("$type", property),
                    "missing"
                })
            }),
            then,    // se for null ou não existir
            orElse,  // caso contrário
        });
    }

    public static BsonDocument Concat(List<BsonValue> values)
    {
        BsonArray arrayToConcat = [];
        foreach(BsonValue value in values) arrayToConcat.Add(value);
        return new("$concat", arrayToConcat);

    }
    
    public static BsonDocument Lookup(string from, string asName, dynamic[][] fields, int? limit = null)
    {
        BsonDocument let = [];
        BsonArray match = [];
        for (int i = 0; i < fields.Length; i++)
        {
            let.Add($"field{i}", fields[i][0]);

            match.Add(new BsonDocument("$eq", new BsonArray { $"$$field{i}", fields[i][1] }));
        }

        BsonDocument lookup = new("$lookup", new BsonDocument{
                    {"from", from},
                    {"let", let},
                    {"pipeline", new BsonArray{
                        new BsonDocument("$match", new BsonDocument("$expr",
                            new BsonDocument("$and", match)
                        )),
                        new BsonDocument("$addFields", new BsonDocument{
                            {"id", MongoHelper.ToString("$_id")},
                        })
                    }},
                    {"as", asName}
                });
        if(limit is not null) 
            lookup["$lookup"]["pipeline"].AsBsonArray.Add(new BsonDocument("$limit", limit));

        return lookup;
    }
}