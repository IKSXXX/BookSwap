namespace BookExchange.Web.Entities;

public class BookOwner : BaseEntity
{
    public int BookId { get; set; }
    public Book? Book { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public bool IsPrimary { get; set; }
}
