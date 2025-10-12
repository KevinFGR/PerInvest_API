namespace PerInvest_API.src.Dtos.Shared;

public class DeleteRequest : RequestBase
{
    public string Id { get; set; } = string.Empty;

    public DeleteRequest() { } 
    public DeleteRequest(HttpContext httpContext, string id) 
    {
        Id = id;
    }    

}