using System.Reflection;
using MongoDB.Bson;

namespace PerInvest_API.src.Helpers;

public static class HelperPerInvest
{
    public static TModel Map<TRequest, TModel>(TRequest request) where TModel : new()
    {
        if (request == null) return new();

        Type requestType = typeof(TRequest);
        Type modelType = typeof(TModel);
        TModel modelInstance = new();

        PropertyInfo[] requestProperties = requestType.GetProperties();
        PropertyInfo[] modelProperties = modelType.GetProperties();

        foreach (PropertyInfo modelProp in modelProperties)
        {
            PropertyInfo? requestProp = requestProperties.FirstOrDefault(
                request => request.Name == modelProp.Name && request.CanRead && modelProp.CanWrite
            );
            if (requestProp != null)
            {
                Type propModelType = modelProp.PropertyType;
                Type propRequestType = requestProp.PropertyType;
                var value = requestProp.GetValue(request);

                if((propModelType == propRequestType ||
                    Nullable.GetUnderlyingType(propModelType) == propRequestType ||
                    propModelType == Nullable.GetUnderlyingType(propRequestType)
                ) && value is not null)
                {
                    modelProp.SetValue(modelInstance, value);
                }
                else if (IsTypeOrNullableOf<string>(propRequestType) && IsTypeOrNullableOf<ObjectId>(propModelType) && value is not null)
                {
                    modelProp.SetValue(modelInstance, new ObjectId(value.ToString()));
                }
                else if (IsTypeOrNullableOf<string>(propRequestType) && IsTypeOrNullableOf<DateTime>(propModelType) && value is not null)
                {
                    modelProp.SetValue(modelInstance, DateTime.Parse(value!.ToString()!));
                }
                // else if (IsTypeOrNullableOf<JsonElement>(propRequestType) && IsTypeOrNullableOf<BsonDocument>(propModelType))
                // {
                //     JsonElement? jsonValue = requestProp.GetValue(request) as JsonElement?;
                //     BsonDocument bsonValue = NormalizeObject(jsonValue);
                //     modelProp.SetValue(modelInstance, bsonValue);
                // }
            }
        }
        return modelInstance;
    }

    public static TModel MapV2<TModel>(this object request) where TModel : new()
    {
        if (request == null) return new();

        Type requestType = request.GetType();
        Type modelType = typeof(TModel);
        TModel modelInstance = new();

        PropertyInfo[] requestProperties = requestType.GetProperties();
        PropertyInfo[] modelProperties = modelType.GetProperties();

        foreach (PropertyInfo modelProp in modelProperties)
        {
            PropertyInfo? requestProp = requestProperties.FirstOrDefault(
                prop => prop.Name == modelProp.Name && prop.CanRead && modelProp.CanWrite
            );

            if (requestProp != null)
            {
                Type propModelType = modelProp.PropertyType;
                Type propRequestType = requestProp.PropertyType;
                var value = requestProp.GetValue(request);

                if ((propModelType == propRequestType ||
                    Nullable.GetUnderlyingType(propModelType) == propRequestType ||
                    propModelType == Nullable.GetUnderlyingType(propRequestType)
                ) && value is not null)
                {
                    modelProp.SetValue(modelInstance, value);
                }
                else if (IsTypeOrNullableOf<string>(propRequestType) && IsTypeOrNullableOf<ObjectId>(propModelType) && value is not null)
                {
                    modelProp.SetValue(modelInstance, new ObjectId(value.ToString()));
                }
                else if (IsTypeOrNullableOf<string>(propRequestType) && IsTypeOrNullableOf<DateTime>(propModelType) && value is not null)
                {
                    modelProp.SetValue(modelInstance, DateTime.Parse(value.ToString()!));
                }
                // else if (IsTypeOrNullableOf<JsonElement>(propRequestType) && IsTypeOrNullableOf<BsonDocument>(propModelType))
                // {
                //     JsonElement? jsonValue = requestProp.GetValue(request) as JsonElement?;
                //     BsonDocument bsonValue = NormalizeObject(jsonValue);
                //     modelProp.SetValue(modelInstance, bsonValue);
                // }
            }
        }

        return modelInstance;
    }

    public static bool IsTypeOrNullableOf<TData>(Type type)
    {
        return (Nullable.GetUnderlyingType(type) ?? type) == typeof(TData);
    }
}