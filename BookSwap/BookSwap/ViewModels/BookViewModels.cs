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
