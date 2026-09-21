using ecommerce.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Application.Common.Extensions;

public static class QueryableExtension
{
    public static async Task<PagedResult<T>> ToPageResultAsync<T>(this IQueryable<T> queryable, int pageNumber,
        int pageSize, CancellationToken ct)
    {
        var count = await queryable.CountAsync(ct);

        var skip = (pageNumber - 1) * pageSize;

        var listItems = await queryable.Skip(skip).Take(pageSize).ToListAsync(ct);

        return new PagedResult<T>(listItems, count, pageNumber, pageSize);
    }
}