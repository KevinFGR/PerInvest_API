using PerInvest_API.src.Common;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.AddDbConfiguration();
builder.AddDataContexts();

WebApplication app = builder.Build();

app.AddSwagger();
app.UseHttpsRedirection();
app.MapEndpoints();

app.Run();