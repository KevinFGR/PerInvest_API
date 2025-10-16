using System.ComponentModel.DataAnnotations;
using PerInvest_API.src.Dtos.Shared;
using PerInvest_API.src.Helpers;

namespace PerInvest_API.src.Dtos;

public class CreateCryptoDto : RequestBase
{
    [Required(ErrorMessage = "Informe a Descrição")]
    public string Description { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;
}

public class UpdateCryptoDto : CreateCryptoDto
{
    [Required(ErrorMessage = "Informe a Identidicação da Crypto")]
    [ObjectIdValid(ErrorMessage = "Identificação da crypto inválida")]
    public string Id { get; set; } = string.Empty;
}