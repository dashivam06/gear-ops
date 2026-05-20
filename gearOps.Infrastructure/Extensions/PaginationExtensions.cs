using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using gearOps.Application.DTOs;

namespace gearOps.Infrastructure.Extensions;

/// <summary>
/// Extension methods for applying pagination to IQueryable sources using EF Core.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Applies pagination to an IQueryable and returns a PagedResult.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query, PaginationParams paging)
    {
        var totalItems = await query.CountAsync();
        var items = await query
            .Skip(paging.Skip)
            .Take(paging.PageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            Page = paging.Page,
            PageSize = paging.PageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)paging.PageSize)
        };
    }
}
