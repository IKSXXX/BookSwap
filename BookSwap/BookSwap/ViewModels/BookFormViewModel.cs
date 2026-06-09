using System.ComponentModel.DataAnnotations;
using BookSwap.Db.Entities;

namespace BookSwap.Web.ViewModels;

public class BookFormViewModel
{
    public int? Id { get; set; }
    [Required(ErrorMessage = "Введите название")]
    public string Title { get; set; } = string.Empty;
    [Required(ErrorMessage = "Введите автора")]
    public string Author { get; set; } = string.Empty;
    public string? ISBN { get; set; }
    public string? Description { get; set; }
    public string Genre { get; set; } = string.Empty;
    public BookCondition Condition { get; set; } = BookCondition.Good;
    public int? Year { get; set; }
    public string? Language { get; set; } = "Русский";
    public IFormFile? CoverImage { get; set; }
    public string? ExistingCoverPath { get; set; }
}
