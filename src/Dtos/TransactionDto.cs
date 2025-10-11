using System.ComponentModel.DataAnnotations;
using PerInvest_API.src.Dtos.Shared;

namespace PerInvest_API.src.Dtos;

public class CreateTransactionDto : RequestBase
{
    [Required(ErrorMessage = "Informe a data")]
    public DateTime? Date { get; set; }

    [Required(ErrorMessage = "Informe o tipo da transação")]
    [MaxLength(15, ErrorMessage = "Tipo deve ter máximo de 15 caracteres")]
    public string Type { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a moeda")]
    public string IdCripto { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o valor")]
    public decimal? Value { get; set; }

    [Required(ErrorMessage = "Informe a cotação da moeda")]
    public decimal? Quotation { get; set; }

    public decimal Tax { get; set; }

    [Required(ErrorMessage = "Informe se é uma compra ou venda")]
    public bool? Selled { get; set; }
}

public class UpdateTransactionDto : CreateTransactionDto
{
    [Required(ErrorMessage = "Informe a identificação da movimentação")]
    public string Id { get; set; } = string.Empty;
}