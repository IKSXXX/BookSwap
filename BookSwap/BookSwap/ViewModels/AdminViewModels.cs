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

public class QuizQuestionFormViewModel
{
    public int? Id { get; set; }
    public int BookId { get; set; }
    public string Quote { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Option2 { get; set; } = string.Empty;
    public string Option3 { get; set; } = string.Empty;
    public string Option4 { get; set; } = string.Empty;
    public List<BookCardViewModel> AvailableBooks { get; set; } = new();
}

public class SetBookOfDayViewModel
{
    public DateTime Date { get; set; } = DateTime.Now.Date;
    public int BookId { get; set; }
    public List<BookCardViewModel> AvailableBooks { get; set; } = new();
    public int? CurrentBookOfDayId { get; set; }
}
