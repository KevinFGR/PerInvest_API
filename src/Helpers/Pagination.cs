using System.Reflection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace PerInvest_API.src.Helpers;

public class Pagination<TModel>
{

    public int Page { get; private set; } = 1;

    public int PageSize { get; private set; } = 10;

    public FilterDefinition<TModel> Filter { get; private set; }

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
            string key = property.Key.ToLower().FirstCharToUpper();
            dynamic value = property.Value.ToString();

            if (key.ToLower() is "page" or "pagesize" or "sort" or "order") continue;

            if (value.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                filters.Add(builder.Eq(key, BsonNull.Value));
                continue;
            }
            if (string.IsNullOrWhiteSpace(value)) continue;

            PropertyInfo? propInfo = typeof(TModel).GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (propInfo == null) continue;
            
            object? convertedValue = ConvertValue(value, propInfo.PropertyType);
            if (convertedValue != null) filters.Add(builder.Eq(key, convertedValue));

            // if(DateTime.TryParse(value, out DateTime dateValue)) value = dateValue;
            // if(int.TryParse(value, out int intValue)) value = intValue;
            // if(decimal.TryParse(value, out decimal decimalValue)) value = decimalValue;
            // filters.Add(builder.Eq(key, value));
        }

        return filters.Count > 0 ? builder.And(filters) : builder.Empty;
    }

    private SortDefinition<TModel>? BuildSort(IQueryCollection query)
    {
        SortDefinitionBuilder<TModel> builder = Builders<TModel>.Sort;
        string? sortField = query.ContainsKey("sort") ? query["sort"].ToString() : null;
        string order = query.ContainsKey("order") ? query["order"].ToString().ToLower() : "asc";

        if (string.IsNullOrWhiteSpace(sortField)) return null;

        return order == "desc"
            ? builder.Descending(sortField.FirstCharToUpper())
            : builder.Ascending(sortField.FirstCharToUpper());
    }


    private object? ConvertValue(string value, Type targetType)
    {
        try
        {
            if (targetType == typeof(string)) return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.TryParse(value, out var i) ? i : null;

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return decimal.TryParse(value, out var d) ? d : null;

            if (targetType == typeof(double) || targetType == typeof(double?))
                return double.TryParse(value, out var d) ? d : null;

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                return DateTime.TryParse(value, out var dt) ? dt : null;

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.TryParse(value, out var b) ? b : null;

            if (targetType.IsEnum)
                return Enum.TryParse(targetType, value, true, out var e) ? e : null;

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

}