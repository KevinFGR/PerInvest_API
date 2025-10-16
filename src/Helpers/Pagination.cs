using System.Globalization;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PerInvest_API.src.Helpers;

public class Pagination<TModel>
{

    public int Page { get; private set; } = 1;

    public int PageSize { get; private set; } = 10;

    public FilterDefinition<TModel> Filter { get; private set; }

    public BsonDocument Match { get; private set; } = new("$match", new BsonDocument("$and", new BsonArray()));

    public SortDefinition<TModel>? Sort { get; private set; }

    public int Skip => (Page - 1) * PageSize;

    public int Limit => PageSize;

    public Pagination(HttpContext httpContext)
    {
        IQueryCollection query = httpContext.Request.Query;

        Page = int.TryParse(query["page"], out var page) && page > 0 ? page : 1;
        PageSize = int.TryParse(query["pageSize"], out var size) && size > 0 ? size : 10;
        Filter = BuildFilter(query);
        Sort = BuildSort(query);
    }

    private FilterDefinition<TModel> BuildFilter(IQueryCollection query)
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
                Match["$match"].AsBsonDocument.Add(key, BsonNull.Value);
                continue;
            }
            // if (string.IsNullOrWhiteSpace(value)) continue;

            PropertyInfo? propInfo = typeof(TModel).GetProperty(linqKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if(propInfo is null)
            {
                filters.Add(builder.Eq(linqKey, value));
                Match["$match"].AsBsonDocument.Add(key, value);
                continue;
            }

            object? convertedValue = ConvertValue(value, propInfo.PropertyType);

            if(propInfo.PropertyType == typeof(DateTime) || propInfo.PropertyType == typeof(DateTime?))
            {
                filters.Add(builder.Gte(linqKey, (convertedValue as DateTime[])![0]));
                filters.Add(builder.Lt(linqKey, (convertedValue as DateTime[])![1]));

                Match["$match"]["$and"].AsBsonArray.Add( new BsonDocument( 
                    $"{key}", new BsonDocument("$gte" , (convertedValue as DateTime[])![0]) 
                ));
                Match["$match"]["$and"].AsBsonArray.Add( new BsonDocument( 
                    $"{key}" , new BsonDocument("$lt", (convertedValue as DateTime[])![1])
                ));
            }
            else
            {
                filters.Add(builder.Eq(linqKey, convertedValue));
                Match["$match"].AsBsonDocument.Add(key, BsonValue.Create(convertedValue));
            }
        }

        if(Match["$match"]["$and"].AsBsonArray.Count == 0) 
            Match["$match"].AsBsonDocument.Remove("$and");

        return filters.Count > 0 ? builder.And(filters) : builder.Empty;
    }

    private static SortDefinition<TModel>? BuildSort(IQueryCollection query)
    {
        SortDefinitionBuilder<TModel> builder = Builders<TModel>.Sort;
        string? sortField = query.ContainsKey("sort") ? query["sort"].ToString() : null;
        string order = query.ContainsKey("order") ? query["order"].ToString().ToLower() : "asc";

        if (string.IsNullOrWhiteSpace(sortField)) return null;

        return order == "desc"
            ? builder.Descending(sortField.FirstCharToUpper())
            : builder.Ascending(sortField.FirstCharToUpper());
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