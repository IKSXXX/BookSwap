using System.ComponentModel.DataAnnotations;

namespace BookExchange.Web.Entities;

public class DiscussionMessage : BaseEntity
{
    public int DiscussionId { get; set; }
    public Discussion? Discussion { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    [Required, MaxLength(2000)]
    public string Text { get; set; } = string.Empty;
}
