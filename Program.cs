using PerInvest_API.src.Common;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddDbConfiguration();
builder.AddDataContexts();
builder.ConfigureAuthentication();
builder.Services.AddAuthorization();

WebApplication app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.AddSwagger();
app.UseHttpsRedirection();
app.MapEndpoints();

app.Run();