using System.ComponentModel.DataAnnotations;

namespace PerInvest_API.src.Dtos;

public class AuthDto
{
    [Required(ErrorMessage = "Informe o email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha")]
    public string Password { get; set; } = string.Empty;
}