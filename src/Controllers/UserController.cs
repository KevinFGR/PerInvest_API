using MongoDB.Driver;
using PerInvest_API.src.Models;
using PerInvest_API.src.Data;
using PerInvest_API.src.Dtos;
using PerInvest_API.src.Models.Users;
using System.Linq.Expressions;
using PerInvest_API.src.Dtos.Shared;
using PerInvest_API.src.Helpers;
using PerInvest_API.src.Common;
using MongoDB.Driver.Linq;

namespace PerInvest_API.src.Controllers;

public class UserController :IEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", Get).RequireAuthorization();
        app.MapGet("/{id}", GetById).RequireAuthorization();
        app.MapPost("/", Add).RequireAuthorization().WithDataAnnotation<CreateUserDto>();
        app.MapPut("/", Update).RequireAuthorization().WithDataAnnotation<UpdateUserDto>();
        app.MapDelete("/{id}", Delete).RequireAuthorization();
    }

    public static async Task<IResult> Get(AppDbContext context, HttpContext httpContext)
    {
        try
        {
            Pagination<User> pagination = new(httpContext);
            var data = await context.Users
                .Find(pagination.Filter)
                .Sort(pagination.Sort)
                .Skip(pagination.Skip)
                .Limit(pagination.Limit)
                .Project(x => new {
                    x.Id,
                    x.Name,
                    x.Email
                })
                .ToListAsync();
            long count = await context.Users.CountDocumentsAsync(pagination.Filter);

            return new PagedResponse<User>(pagination, data, count).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter usuário: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> GetById(AppDbContext context, HttpContext httpContext, string id)
    {
        try
        {
            User data = await context.Users
                .Find(x=> x.Id ==id && !x.Deleted)
                .FirstOrDefaultAsync();

            return new Response(data).Result;
        }
        catch (Exception ex)
        {
            return new Response(500, $"Falha ao obter usuário: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Add(AppDbContext context, CreateUserDto request)
    {
        try
        {
            User user = request.Map<User>();
            user.Email = user.Email.ToLower();
            
            User? userWithSameEmail = await context.Users.Find(x => x.Email.Equals(user.Email) && !x.Deleted).FirstOrDefaultAsync();
            if(userWithSameEmail is not null)
                return new Response(400, "Já existe um usuário com este email").Result;

            (user.Salt, user. Hash) = HelperPerInvest.GenerateHash(request.Password);
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            user.CreatedBy = request.UserId;
            user.UpdatedBy = request.UserId;

            await context.Users.InsertOneAsync(user);

            return new Response(201, "Usuário criado com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response(500, $"Falha ao salvar usuário: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Update(AppDbContext context, UpdateUserDto request)
    {
        try
        {
            User? user = await context.Users.Find(x => x.Id == request.Id && !x.Deleted).FirstOrDefaultAsync();
            if(user is null) return new Response(400, "Usuário não encontrado").Result;
            
            user.Email = request.Email.ToLower();
            user.Name = request.Name;
            user.UpdatedBy = request.UserId;
            user.UpdatedAt = DateTime.Now;

            Expression<Func<User, bool>> filter = x => x.Id == request.Id && !x.Deleted;
            await context.Users.ReplaceOneAsync(filter, user);

            return new Response(200, "Usuário atualizado com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response(500, $"Falha ao atualizar usuário: {ex.Message}").Result;
        }
    }

    public static async Task<IResult> Delete(AppDbContext context, HttpContext httpContext, string id)
    {
        try
        {
            DeleteRequest request = new (httpContext, id);
            Expression<Func<User, bool>> filter = x => x.Id == id;
            var update = Builders<User>.Update
                .Set(x => x.Deleted, true)
                .Set(x => x.DeletedAt, DateTime.Now)
                .Set(x => x.DeletedBy, request.UserId);
            await context.Users.UpdateOneAsync(filter, update);

            return new Response(200, "Usuário excluido com sucesso").Result;
        }
        catch(Exception ex)
        {
            return new Response(500, $"Falha ao excluir usuário: {ex.Message}").Result;
        }
    }
}