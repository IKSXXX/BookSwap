namespace BookExchange.Db.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = "exchange";
    public string Text { get; set; } = string.Empty;
    public string? RelatedUrl { get; set; }
    public bool IsRead { get; set; }

    public User? User { get; set; }
}
