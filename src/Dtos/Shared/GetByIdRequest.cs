using System.ComponentModel.DataAnnotations;

namespace PerInvest_API.src.Dtos.Shared;

public class GetByIdRequest : RequestBase
{
    [Required(ErrorMessage = "Informe a identificação do registro")]
    public string Id { get; set; } = string.Empty;
}