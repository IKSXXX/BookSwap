using BookExchange.Web.Entities;

namespace BookExchange.Web.ViewModels;

public class BookCardViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string ConditionLabel { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public OwnerSummaryViewModel Owner { get; set; } = new();
}

public class OwnerSummaryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public double Rating { get; set; }
}

public class BookCatalogViewModel
{
    public List<BookCardViewModel> Books { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
    public string? SearchQuery { get; set; }
    public List<string> AllGenres { get; set; } = new();
    public List<string> SelectedGenres { get; set; } = new();
    public List<BookCondition> SelectedConditions { get; set; } = new();
    public bool OnlyAvailable { get; set; }
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public bool HasPrev => Page > 1;
    public bool HasNext => Page < TotalPages;
}
