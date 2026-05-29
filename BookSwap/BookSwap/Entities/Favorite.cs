namespace BookExchange.Web.Entities;

public class Favorite : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public int BookId { get; set; }
    public Book? Book { get; set; }

    public bool IsWishlist { get; set; }
}
