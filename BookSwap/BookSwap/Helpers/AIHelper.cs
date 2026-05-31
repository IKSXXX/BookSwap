using BookExchange.Db.Entities;

namespace BookExchange.Web.Helpers;

public static class AIHelper
{
    public static IEnumerable<Book> GetRecommendations(string query, IEnumerable<Book> books, int take = 6)
    {
        if (string.IsNullOrWhiteSpace(query)) return books.Take(take);

        var tokens = query.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToArray();

        if (tokens.Length == 0) return books.Take(take);

        return books
            .Select(b => new { b, score = Score(b, tokens) })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(take)
            .Select(x => x.b);
    }

    static int Score(Book b, string[] tokens)
    {
        var haystack = $"{b.Title} {b.Author} {b.Description} {b.Genre}".ToLowerInvariant();
        return tokens.Count(t => haystack.Contains(t));
    }
}
