namespace BookSwap.Web.Helpers;

public record GoogleBookResult(string Title, string Author, string? Description, string? CoverImageUrl, int? Year);

public static class GoogleBooksHelper
{
    static readonly Dictionary<string, GoogleBookResult> Mock = new()
    {
        ["9785170908691"] = new("1984", "Джордж Оруэлл", "Антиутопия о тоталитарном обществе", null, 1949),
        ["9785699801234"] = new("Мастер и Маргарита", "Михаил Булгаков", "Мистический роман", null, 1967),
    };

    public static Task<GoogleBookResult?> FetchByISBNAsync(string isbn)
    {
        var clean = isbn.Replace("-", "").Trim();
        GoogleBookResult? result = Mock.GetValueOrDefault(clean);
        return Task.FromResult(result);
    }
}
