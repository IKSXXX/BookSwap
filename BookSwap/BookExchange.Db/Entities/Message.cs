using System.ComponentModel.DataAnnotations;

namespace BookSwap.Db.Entities;

public class Message : BaseEntity
{
    public int ExchangeRequestId { get; set; }
    public ExchangeRequest? ExchangeRequest { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public User? Sender { get; set; }

    [Required, MaxLength(5000)]
    public string Text { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
