using System.ComponentModel.DataAnnotations;
using PerInvest_API.src.Dtos.Shared;
using PerInvest_API.src.Helpers;

namespace PerInvest_API.src.Dtos;

public class CreateUserDto : RequestBase
{
    [Required(ErrorMessage = "Informe o nome")]
    [MaxLength(100, ErrorMessage = "Nome deve ter máximo de 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o email")]
    [MaxLength(100, ErrorMessage = "Email deve ter máximo de 100 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    [MaxLength(30, ErrorMessage = "Senha deve ter máximo de 30 caracteres")]
    public string Password { get; set; } = string.Empty;
}

public class UpdateUserDto : RequestBase
{
    [Required(ErrorMessage = "Informe a Identidicação do usuário")]
    [ObjectIdValid(ErrorMessage = "Identificação da usuario inválida")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o nome")]
    [MaxLength(100, ErrorMessage = "Nome deve ter máximo de 100 caracteres")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o email")]
    [MaxLength(100, ErrorMessage = "Email deve ter máximo de 100 caracteres")]
    public string Email { get; set; } = string.Empty;
}