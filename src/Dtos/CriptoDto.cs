using System.ComponentModel.DataAnnotations;

namespace PerInvest_API.src.Dtos;

public class CreateCriptoDto : RequestBase
{
    [Required(ErrorMessage = "Informe a Descrição")]
    public string Description { get; set; } = string.Empty;
}

public class UpdateCriptoDto : CreateCriptoDto
{
    [Required(ErrorMessage = "Informe a Identidicação da Cripto")]
    public string Id { get; set; } = string.Empty;
}