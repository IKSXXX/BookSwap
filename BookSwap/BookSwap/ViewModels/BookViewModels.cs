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

public class BookDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string? ISBN { get; set; }
    public string? Description { get; set; }
    public string? CoverImagePath { get; set; }
    public string Genre { get; set; } = string.Empty;
    public string ConditionLabel { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string? Language { get; set; }
    public bool IsAvailable { get; set; }
    public OwnerSummaryViewModel Owner { get; set; } = new();
    public List<OwnerSummaryViewModel> Owners { get; set; } = new();
    public List<BookCardViewModel> Similar { get; set; } = new();
}

public class DiscussionDetailsViewModel
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public List<DiscussionMessageViewModel> Messages { get; set; } = new();
}

public class DiscussionMessageViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string? AvatarPath { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
