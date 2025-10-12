using System.ComponentModel.DataAnnotations;
using MongoDB.Bson;

namespace PerInvest_API.src.Helpers;

public class ObjectIdValid : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not null && value is string strValue)
            if (!ObjectId.TryParse(strValue, out _)) return new ValidationResult(ErrorMessage);

        return ValidationResult.Success;
    }
}