namespace ecommerce.Application.Common.Interfaces;

public interface IPagedQuery
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}