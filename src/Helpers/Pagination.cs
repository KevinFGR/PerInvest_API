using System.Globalization;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PerInvest_API.src.Helpers;

public class Pagination<TModel>
{

    public int Page { get; private set; } = 1;

    public int PageSize { get; private set; } = 10;

    public string Order { get; set; } = string.Empty;

    public string SortString { get; set; } = string.Empty;

    public FilterDefinition<TModel> Filter { get; set; } = Builders<TModel>.Filter.Empty;

    public SortDefinition<TModel>? Sort { get; set; }

    public int Skip => (Page - 1) * PageSize;

    public int Limit => PageSize;

    public BsonDocument BsonFilter { get; private set; } = new("$match", new BsonDocument("$and", new BsonArray()));

    public BsonDocument BsonSort { get; private set; } = [];

    public BsonDocument BsonSkip => new("$skip", Skip);

    public BsonDocument BsonLimit => new("$limit", PageSize);

    public Pagination(HttpContext httpContext)
    {
        IQueryCollection query = httpContext.Request.Query;

        Page = int.TryParse(query["page"], out var page) && page > 0 ? page : 1;
        PageSize = int.TryParse(query["pageSize"], out var size) && size > 0 ? size : 10;
        BuildFilter(query);
        BuildSort(query);
    }

    private void BuildFilter(IQueryCollection query)
    {
        FilterDefinitionBuilder<TModel> builder = Builders<TModel>.Filter;
        List<FilterDefinition<TModel>> filters = [];

        foreach (var property in query)
        {
            string linqKey = property.Key.FirstCharToUpper();
            string key = property.Key;
            dynamic value = property.Value.ToString();

            if (key.ToLower() is "page" or "pagesize" or "sort" or "order") 
                continue;

            if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add(builder.Eq(linqKey, BsonNull.Value));
                BsonFilter["$match"].AsBsonDocument.Add(key, BsonNull.Value);
                continue;
            }
            // if (string.IsNullOrWhiteSpace(value)) continue;

            PropertyInfo? propInfo = typeof(TModel).GetProperty(linqKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if(propInfo is null)
            {
                filters.Add(builder.Eq(linqKey, value));
                BsonFilter["$match"].AsBsonDocument.Add(key, value);
                continue;
            }

            object? convertedValue = ConvertValue(value, propInfo.PropertyType);

            if(propInfo.PropertyType == typeof(DateTime) || propInfo.PropertyType == typeof(DateTime?))
            {
                filters.Add(builder.Gte(linqKey, (convertedValue as DateTime[])![0]));
                filters.Add(builder.Lt(linqKey, (convertedValue as DateTime[])![1]));

                BsonFilter["$match"]["$and"].AsBsonArray.Add( new BsonDocument( 
                    $"{key}", new BsonDocument("$gte" , (convertedValue as DateTime[])![0]) 
                ));
                BsonFilter["$match"]["$and"].AsBsonArray.Add( new BsonDocument( 
                    $"{key}" , new BsonDocument("$lt", (convertedValue as DateTime[])![1])
                ));
            }
            else
            {
                filters.Add(builder.Eq(linqKey, convertedValue));
                BsonFilter["$match"].AsBsonDocument.Add(key, BsonValue.Create(convertedValue));
            }
        }

        if(BsonFilter["$match"]["$and"].AsBsonArray.Count == 0) 
            BsonFilter["$match"].AsBsonDocument.Remove("$and");

        Filter = filters.Count > 0 ? builder.And(filters) : builder.Empty;
    }

    private void BuildSort(IQueryCollection query)
    {
        SortDefinitionBuilder<TModel> builder = Builders<TModel>.Sort;
        Order = query.ContainsKey("sort") ? query["sort"].ToString() : "createdAt";
        SortString = query.ContainsKey("order") ? query["order"].ToString().ToLower() : "asc";

        Sort = SortString == "desc"
            ? builder.Descending(Order.FirstCharToUpper())
            : builder.Ascending(Order.FirstCharToUpper());

        BsonSort = new("$sort", new BsonDocument($"{Order}", SortString == "desc" ? -1 : 1));
    }

    private static object? ConvertValue(string value, Type targetType)
    {
        try
        {
            if (targetType == typeof(string)) return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.TryParse(value, out var intValue) ? intValue : null;

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return decimal.TryParse(value, out var decimalValue) ? decimalValue : null;

            if (targetType == typeof(double) || targetType == typeof(double?))
                return double.TryParse(value, out var doubleValue) ? doubleValue : null;

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                string[] formatos = ["yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss.fffZ"];
                if (DateTime.TryParseExact(value, formatos, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dateValue))
                    // return dateValue.Date.AddDays(-1);
                    return new DateTime[]{ dateValue.Date.AddDays(1), dateValue.Date.AddDays(2)};
                return null;
            }

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.TryParse(value, out var booleanValue) ? booleanValue : null;

            if (targetType.IsEnum)
                return Enum.TryParse(targetType, value, true, out var enumValue) ? enumValue : null;

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

}