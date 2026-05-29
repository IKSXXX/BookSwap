using Microsoft.AspNetCore.Identity;

namespace BookExchange.Web.Entities;

public class User : IdentityUser
{
    public string? AvatarPath { get; set; }
    public string? Location { get; set; }
    public double Rating { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public bool IsBlocked { get; set; }
}
