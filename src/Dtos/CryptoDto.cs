using System.ComponentModel.DataAnnotations;
using PerInvest_API.src.Dtos.Shared;
using PerInvest_API.src.Helpers;

namespace PerInvest_API.src.Dtos;

public class CreateCryptoDto : RequestBase
{
    [Required(ErrorMessage = "Informe a Descrição")]
    [MaxLength(100, ErrorMessage = "Descrição deve ter máximo de 100 caracteres")]
    public string Description { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Cor deve ter máximo de 100 caracteres")]
    public string Color { get; set; } = string.Empty;
}

public class UpdateCryptoDto : CreateCryptoDto
{
    [Required(ErrorMessage = "Informe a Identidicação da Crypto")]
    [ObjectIdValid(ErrorMessage = "Identificação da crypto inválida")]
    public string Id { get; set; } = string.Empty;
}