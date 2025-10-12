namespace PerInvest_API.src.Dtos.Shared;

public class RequestBase
{
    public string? UserId { get; set; }

    public string Token { get; set; } = string.Empty;
}