using System.ComponentModel.DataAnnotations;

namespace BookExchange.Db.Entities;

public class Review : BaseEntity
{
    public string FromUserId { get; set; } = string.Empty;
    public User? FromUser { get; set; }
    public string ToUserId { get; set; } = string.Empty;
    public User? ToUser { get; set; }
    public int? Rating { get; set; }

    [MaxLength(2000)]
    public string? Comment { get; set; }
    public int ExchangeRequestId { get; set; }
    public ExchangeRequest? ExchangeRequest { get; set; }
}
