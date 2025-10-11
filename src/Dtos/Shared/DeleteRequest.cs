using System.ComponentModel.DataAnnotations;

namespace PerInvest_API.src.Dtos.Shared;

public class DeleteRequest : RequestBase
{
    [Required(ErrorMessage = "Informe a identificação do registro")]
    public string? Id { get; set; }
}