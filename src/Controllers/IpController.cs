namespace PerInvest_API.src.Controllers;

public class IpController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetIp).Produces<dynamic>();
    }

    public static async Task<dynamic> GetIp()
    {
        try
        {
            using HttpClient httpClient = new();
            dynamic data = new
            {
                // ip = await httpClient.GetStringAsync("https://api.ipify.org")
                ip = "localhost:5000 kkkkkkkkk"
            };
            return new { success = true, data };
        }
        catch (Exception ex)
        {
            return new { success = false, message = $"Falha ao obter Ip: {ex.Message}" };
        }
    }
}