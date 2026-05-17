using PerInvest_API.src.Models;

namespace PerInvest_API.src.Helpers;

public static class HelperHttp
{
    public async static Task<Response> Get(IHttpClientFactory httpClientFactory, string uri)
    {
        HttpClient client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "User-Agent",
            "apitest/1.0"
        );

        HttpResponseMessage result = await client.GetAsync(uri);
        string? content = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
            return new Response((int)result.StatusCode, content);

        object? json = await result.Content.ReadFromJsonAsync<object>();
        return new Response(json);
    }
    
    public async static Task<Response> GetString(IHttpClientFactory httpClientFactory, string uri){
        HttpClient client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add(
            "User-Agent",
            "apitest/1.0"
        );

        HttpResponseMessage result = await client.GetAsync( uri );
        string? content = await result.Content.ReadAsStringAsync();

        if (!result.IsSuccessStatusCode)
            return new Response((int)result.StatusCode, content);

        return new Response(200, "", content);
    }
}