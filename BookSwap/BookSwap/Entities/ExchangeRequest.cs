namespace BookExchange.Web.Entities;

public class ExchangeRequest : BaseEntity
{
    public int? BookOfferedId { get; set; }
    public Book? BookOffered { get; set; }

    public int BookRequestedId { get; set; }
    public Book? BookRequested { get; set; }

    public string SenderId { get; set; } = string.Empty;
    public User? Sender { get; set; }

    public string ReceiverId { get; set; } = string.Empty;
    public User? Receiver { get; set; }

    public ExchangeStatus Status { get; set; } = ExchangeStatus.Pending;

    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
