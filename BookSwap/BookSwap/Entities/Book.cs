using System.ComponentModel.DataAnnotations;

namespace BookExchange.Web.Entities;

public class Book : BaseEntity
{
    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Author { get; set; } = string.Empty;

    [MaxLength(32)]
    public string? ISBN { get; set; }

    [MaxLength(4000)]
    public string? Description { get; set; }

    [MaxLength(500)]
    public string? CoverImagePath { get; set; }

    [MaxLength(100)]
    public string Genre { get; set; } = "Другое";

    public BookCondition Condition { get; set; } = BookCondition.Good;

    public int? Year { get; set; }

    [MaxLength(60)]
    public string? Language { get; set; } = "Русский";

    public bool IsAvailable { get; set; } = true;

    public bool IsHidden { get; set; }

    public ICollection<BookOwner> BookOwners { get; set; } = new List<BookOwner>();
}
