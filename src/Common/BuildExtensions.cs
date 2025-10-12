using PerInvest_API.src.Data;

namespace PerInvest_API.src.Common;

public static class BuildExtensions
{
    public static void AddDbConfiguration(this WebApplicationBuilder builder)
    {
        AppDbContext.ConnectionString = builder.Configuration.GetSection("PerInvDB:ConnectionString").Value;
        AppDbContext.DatabaseName = builder.Configuration.GetSection("PerInvDB:DatabaseName").Value;
        AppDbContext.IsSSL = Convert.ToBoolean(builder.Configuration.GetSection("PerInvDB:IsSSL").Value);
        // Configuration.APIBackend = builder.Configuration.GetValue<string>("BackendUrl") ?? string.Empty;
        // Configuration.FrontendUrl = builder.Configuration.GetValue<string>("FrontendUrl") ?? string.Empty;
    }
    public static void AddDataContexts(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<AppDbContext>();
    }
    // public static void AddCrossOrigin(this WebApplicationBuilder builder)
    // {
    //     builder.Services.AddCors(
    //         options => options.AddPolicy(
    //             "AllowAnyCorsPolicy",
    //             policy => policy
    //                 .WithOrigins([
    //                     Configuration.APIBackend,
    //                     // Configuration.FrontendUrl
    //                 ])
    //                 .AllowAnyMethod()
    //                 .AllowAnyHeader()
    //                 .AllowCredentials()
    //         ));
    // }
}