namespace BookExchange.Web.ViewModels;

public class LoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class UserProfileViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarPath { get; set; }
    public string? Location { get; set; }
    public double Rating { get; set; }
    public DateTime RegistrationDate { get; set; }
    public int BooksCount { get; set; }
    public int CompletedExchanges { get; set; }
    public List<BookCardViewModel> MyBooks { get; set; } = new();
    public List<ExchangeListItemViewModel> Exchanges { get; set; } = new();
    public List<BookCardViewModel> Favorites { get; set; } = new();
    public List<ReviewDisplayViewModel> ReviewsReceived { get; set; } = new();
    public List<ReviewDisplayViewModel> ReviewsGiven { get; set; } = new();
    public bool IsCurrentUser { get; set; }
}

public class ReviewDisplayViewModel
{
    public string FromUserId { get; set; } = string.Empty;
    public string FromUserName { get; set; } = string.Empty;
    public string ToUserId { get; set; } = string.Empty;
    public string ToUserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
