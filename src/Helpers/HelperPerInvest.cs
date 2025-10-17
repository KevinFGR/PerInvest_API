using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using PerInvest_API.src.Models.Users;

namespace PerInvest_API.src.Helpers;

public static class HelperPerInvest
{
    public static TModel Map<TModel>(this object request) where TModel : new()
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

    public static string FirstCharToUpper(this string input, CultureInfo? culture = null)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        culture ??= CultureInfo.CurrentCulture;
        return char.ToUpper(input[0], culture) + input.Substring(1);
    }

    public static dynamic ToDynamic(this BsonDocument document)
    {
        return BsonSerializer.Deserialize<dynamic>(document);
    }

    public static List<dynamic> ToDynamic(this List<BsonDocument> documents)
    {
        return documents.Select(x => x.ToDynamic()).ToList();
    }

    public static string GenerateToken(User user, IConfiguration config)
    {
        string issuer = config["Jwt:Issuer"]!;
        string audience = config["Jwt:Audience"]!;
        string secretKey = config["Jwt:Key"]!;

        SymmetricSecurityKey securityKey = new (Encoding.UTF8.GetBytes(secretKey!));
        SigningCredentials credentials = new (securityKey, SecurityAlgorithms.HmacSha256);

        Claim[] claims = [
            new (JwtRegisteredClaimNames.Sub, user.Id),
            new (JwtRegisteredClaimNames.UniqueName, user.Name),
            new (JwtRegisteredClaimNames.Email, user.Name),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        JwtSecurityToken token = new (
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

        public static (string Salt, string Hash) GenerateHash(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);

        using var sha256 = SHA256.Create();
        byte[] combinedBytes = Encoding.UTF8.GetBytes(password).Concat(saltBytes).ToArray()!;

        byte[] hashBytes = SHA256.HashData(combinedBytes);

        string salt = Convert.ToBase64String(saltBytes);
        string hash = Convert.ToBase64String(hashBytes);

        return (salt, hash);
    }

    public static bool PasswordIsValid(string password, string salt, string hash)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);

        using var sha256 = SHA256.Create();
        byte[] combinedBytes = Encoding.UTF8.GetBytes(password).Concat(saltBytes).ToArray()!;

        byte[] computedHash = SHA256.HashData(combinedBytes);
        string computedHashBase64 = Convert.ToBase64String(computedHash);

        return computedHashBase64 == hash;
    }
}