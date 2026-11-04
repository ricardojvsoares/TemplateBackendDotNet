namespace Domain.Common.Pagination;

public sealed record CursorPaginatedList<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasNextPage);