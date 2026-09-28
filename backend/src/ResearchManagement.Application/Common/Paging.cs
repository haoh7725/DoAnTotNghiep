using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Application.Common;

public class PagedQuery
{
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; set; } = 20;

    public int Skip => (Math.Max(Page, 1) - 1) * Math.Clamp(PageSize, 1, MaxPageSize);
    public int Take => Math.Clamp(PageSize, 1, MaxPageSize);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
