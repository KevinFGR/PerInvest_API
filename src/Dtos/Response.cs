namespace PerInvest.src.Dtos;

public class Response
{
    public int Code { get; set; }

    public bool Success => Code < 300 && Code > 199;

    public string Message { get; set; } = "";

    public dynamic? Data { get; set; }

    public dynamic Json => new { Success, Message, Data };

    public IResult Result => Code switch
    {
        200 => TypedResults.Ok(Json),
        201 => TypedResults.Created(Json),
        204 => TypedResults.NoContent(),
        400 => TypedResults.BadRequest(Json),
        401 => TypedResults.Unauthorized(),
        500 => TypedResults.Json(Json, statusCode: 500),
        _ => TypedResults.BadRequest(Json)
    };

    public Response(dynamic? data)
    {
        Data = data;
        Code = 200;
    }

    public Response(string message)
    {
        Data = null;
        Code = 400;
        Message = message;
    }

    public Response(int code, string message, dynamic? data = null)
    {
        Data = data;
        Code = code;
        Message = message;
    }
}