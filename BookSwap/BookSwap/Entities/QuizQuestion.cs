using System.ComponentModel.DataAnnotations;

namespace BookExchange.Web.Entities;

public class QuizQuestion : BaseEntity
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    [Required, MaxLength(1000)]
    public string Quote { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string CorrectAnswer { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Option2 { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Option3 { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Option4 { get; set; } = string.Empty;
}
