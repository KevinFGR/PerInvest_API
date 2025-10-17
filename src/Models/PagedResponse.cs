using PerInvest_API.src.Helpers;

namespace PerInvest_API.src.Models;

public class PagedResponse<TModel>
{
    public dynamic? Data { get; set; }

    public long TotalCount { get; set; }

    public int PageSize { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public int Page { get; set; }

    public string Sort { get; set; }

    public string Order { get; set; }

    public dynamic Json => new { success = true, Page, PageSize, TotalPages, TotalCount, Sort, Order, Data };

    public IResult Result => TypedResults.Ok(Json);

    public PagedResponse(Pagination<TModel> pagination, dynamic data, long totalCount)
    {
        TotalCount = totalCount;
        PageSize = pagination.PageSize;
        Page = pagination.Page;
        Sort = pagination.SortString;
        Order = pagination.Order;
        Data = data;
    }
}