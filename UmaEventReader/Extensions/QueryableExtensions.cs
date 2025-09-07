using Microsoft.EntityFrameworkCore;
using UmaEventReader.Model;

namespace UmaEventReader.Extensions;

public static class QueryableExtensions {
    public static IQueryable<UmaEvent> WhereEventNameContains(
        this IQueryable<UmaEvent> query, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
            return query;

        return query.Where(e => EF.Functions.ILike(e.EventName, $"%{term}%"));
    }
}