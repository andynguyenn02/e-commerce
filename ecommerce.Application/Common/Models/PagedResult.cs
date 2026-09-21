namespace ecommerce.Application.Common.Models;

public class PagedResult<T>
{
    public PagedResult(List<T> items, int totalItems, int pageNumber, int pageSize)
    {
        Items = items ?? [];
        TotalItems = totalItems;
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
    }

    public List<T> Items { get; set; }
    public int TotalItems { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public int TotalPages { get; set; }
}