namespace BookExchange.Web.ViewModels;

public class AdminStatsViewModel
{
    public int UsersCount { get; set; }
    public int BooksCount { get; set; }
    public int ExchangesCount { get; set; }
    public int ReviewsCount { get; set; }
}

public class AdminUserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public bool IsAdmin { get; set; }
    public double Rating { get; set; }
    public DateTime RegistrationDate { get; set; }
    public int BooksCount { get; set; }
}
