using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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

    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthentication(
            options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }
        ).AddJwtBearer(
            options => options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey
                (
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                ),
                ClockSkew = TimeSpan.Zero
            }
        );
    }
}