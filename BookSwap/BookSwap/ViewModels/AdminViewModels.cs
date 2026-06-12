namespace BookSwap.Web.ViewModels;

public class AdminStatsViewModel
{
    public int UsersCount { get; set; }
    public int BooksCount { get; set; }
    public int ExchangesCount { get; set; }
    public int ReviewsCount { get; set; }
    public int BlockedUsersCount { get; set; }
    public int HiddenBooksCount { get; set; }
    public int PendingExchangesCount { get; set; }
    public int CompletedExchangesCount { get; set; }
    public int QuizQuestionsCount { get; set; }

    public List<AdminRecentUserViewModel> RecentUsers { get; set; } = new();
    public List<AdminRecentBookViewModel> RecentBooks { get; set; } = new();
}

public class AdminRecentUserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
    public bool IsBlocked { get; set; }
}

public class AdminRecentBookViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsHidden { get; set; }
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
    public DateTime Date { get; set; } = DateTime.UtcNow.Date;
    public int BookId { get; set; }
    public List<BookCardViewModel> AvailableBooks { get; set; } = new();
    public int? CurrentBookOfDayId { get; set; }
    public string? CurrentBookTitle { get; set; }
    public string? CurrentBookAuthor { get; set; }
    public string? CurrentBookCover { get; set; }
    public string? CurrentBookGenre { get; set; }
}
