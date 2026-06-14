using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Web.Helpers;
using BookSwap.Db.Interfaces;
using BookSwap.Web.Services;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly GigaChatService _gigaChat;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IUnitOfWork uow, IMapper mapper, GigaChatService gigaChat, ILogger<HomeController> logger)
    {
        _uow = uow;
        _mapper = mapper;
        _gigaChat = gigaChat;
        _logger = logger;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var bookOfTheDay = await _uow.BooksOfTheDay.Query()
            .OrderByDescending(b => b.Date)
            .Include(b => b.Book!).ThenInclude(bk => bk.BookOwners).ThenInclude(bo => bo.User)
            .FirstOrDefaultAsync();

        var recent = await _uow.Books.Query()
            .Where(b => !b.IsHidden)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(6)
            .ToListAsync();

        var quiz = await GetRandomQuizAsync();

        var vm = new HomeViewModel
        {
            BookOfTheDay = bookOfTheDay?.Book != null ? _mapper.Map<BookCardViewModel>(bookOfTheDay.Book) : null,
            RecentBooks = recent.Select(_mapper.Map<BookCardViewModel>).ToList(),
            QuizQuestion = quiz
        };
        return View(vm);
    }

    [HttpGet("/quiz/next")]
    public async Task<IActionResult> QuizNext()
    {
        var quiz = await GetRandomQuizAsync();
        return Json(quiz);
    }

    [HttpPost("/quiz/answer")]
    public async Task<IActionResult> QuizAnswer([FromForm] int questionId, [FromForm] string answer)
    {
        var question = await _uow.QuizQuestions.GetByIdAsync(questionId);
        if (question == null) return NotFound();
        return Json(new QuizAnswerResultViewModel
        {
            IsCorrect = string.Equals(answer?.Trim(), question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase),
            CorrectAnswer = question.CorrectAnswer,
            BookId = question.BookId
        });
    }

    [HttpPost("/ai/recommend")]
    public async Task<IActionResult> AiRecommend([FromForm] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new AiRecommendationViewModel { Query = "", Books = new List<BookCardViewModel>(), Source = "empty" });

        var books = await _uow.Books.Query()
            .Where(b => !b.IsHidden && b.IsAvailable)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .ToListAsync();

        var bookList = books.Select(b => $"{b.Title} — {b.Author} ({b.Genre})").ToList();

        List<BookCardViewModel> result = new();
        string source = "none";

        try
        {
            var recommendedTitles = await _gigaChat.GetBookRecommendationsAsync(query, bookList);
            if (recommendedTitles.Count > 0)
            {
                result = books
                    .Where(b => recommendedTitles.Any(t =>
                        FuzzyMatch(t, b.Title) ||
                        FuzzyMatch(t, b.Author) ||
                        b.Genre.Contains(t, StringComparison.OrdinalIgnoreCase)))
                    .Select(_mapper.Map<BookCardViewModel>)
                    .ToList();

                if (result.Count > 0) source = "gigachat";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GigaChat unavailable, falling back to keyword search");
        }

        if (result.Count == 0)
        {
            var keywords = query.ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '!', '?', '-', '—' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToArray();

            if (keywords.Length > 0)
            {
                var scored = books
                    .Select(b => new { book = b, score = ScoreBook(b, keywords) })
                    .Where(x => x.score > 0)
                    .OrderByDescending(x => x.score)
                    .ToList();

                if (scored.Count > 0)
                {
                    var maxScore = scored[0].score;
                    result = scored
                        .Where(x => x.score >= maxScore / 2)
                        .Take(6)
                        .Select(x => _mapper.Map<BookCardViewModel>(x.book))
                        .ToList();

                    source = "keywords";
                }
            }
        }

        return Json(new AiRecommendationViewModel
        {
            Query = query,
            Books = result,
            Source = source,
            Message = result.Count == 0
                ? $"По запросу \"{query}\" ничего не найдено. Попробуйте другой запрос."
                : null
        });
    }

    static bool FuzzyMatch(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b)) return false;
        a = a.ToLowerInvariant().Trim();
        b = b.ToLowerInvariant().Trim();
        if (a == b) return true;
        if (a.Contains(b) || b.Contains(a)) return true;
        return false;
    }

    static int ScoreBook(Book book, string[] keywords)
    {
        var haystack = $"{book.Title} {book.Author} {book.Description} {book.Genre}".ToLowerInvariant();
        return keywords.Count(k => haystack.Contains(k));
    }

    async Task<QuizQuestionViewModel?> GetRandomQuizAsync()
    {
        var count = await _uow.QuizQuestions.Query().CountAsync();
        if (count == 0) return null;
        var skip = Random.Shared.Next(count);
        var question = await _uow.QuizQuestions.Query().Skip(skip).Take(1).FirstAsync();
        var options = new List<string> { question.CorrectAnswer, question.Option2, question.Option3, question.Option4 };
        options = options.OrderBy(_ => Random.Shared.Next()).ToList();
        return new QuizQuestionViewModel { Id = question.Id, Quote = question.Quote, Options = options, BookId = question.BookId };
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/error")]
    public IActionResult Error() => View();
}
