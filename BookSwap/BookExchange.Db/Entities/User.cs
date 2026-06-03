using System.ComponentModel.DataAnnotations;

namespace BookExchange.Db.Entities;

public class User : Microsoft.AspNetCore.Identity.IdentityUser
{
    public double? Rating { get; set; }

    [MaxLength(100)]
    public string? Location { get; set; }
    public bool IsBlocked { get; set; }

    [MaxLength(500)]
    public string? AvatarPath { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public ICollection<BookOwner> BookOwners { get; set; } = new List<BookOwner>();
    public ICollection<ExchangeRequest> SentRequests { get; set; } = new List<ExchangeRequest>();
    public ICollection<ExchangeRequest> ReceivedRequests { get; set; } = new List<ExchangeRequest>();
    public ICollection<Review> ReviewsGiven { get; set; } = new List<Review>();
    public ICollection<Review> ReviewsReceived { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<Discussion> Discussions { get; set; } = new List<Discussion>();
    public ICollection<DiscussionMessage> DiscussionMessages { get; set; } = new List<DiscussionMessage>();
}
