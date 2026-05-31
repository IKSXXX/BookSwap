namespace BookExchange.Web.ViewModels;

public class HomeViewModel
{
    public BookCardViewModel? BookOfTheDay { get; set; }
    public List<BookCardViewModel> RecentBooks { get; set; } = new();
    public QuizQuestionViewModel? QuizQuestion { get; set; }
}

public class QuizQuestionViewModel
{
    public int Id { get; set; }
    public string Quote { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int BookId { get; set; }
}

public class QuizAnswerResultViewModel
{
    public bool IsCorrect { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public int BookId { get; set; }
}

public class AiRecommendationViewModel
{
    public string Query { get; set; } = string.Empty;
    public List<BookCardViewModel> Books { get; set; } = new();
}
