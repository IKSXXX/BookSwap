using System.ComponentModel.DataAnnotations;

namespace BookExchange.Web.Entities;

public class Discussion : BaseEntity
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public ICollection<DiscussionMessage> Messages { get; set; } = new List<DiscussionMessage>();
}
