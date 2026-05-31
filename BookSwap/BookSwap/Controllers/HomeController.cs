using AutoMapper;
using BookExchange.Db.Entities;
using BookExchange.Web.Helpers;
using BookExchange.Db.Interfaces;
using BookExchange.Web.Services;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly GigaChatService _gigaChat;

    public HomeController(IUnitOfWork uow, IMapper mapper, GigaChatService gigaChat)
    {
        _uow = uow;
        _mapper = mapper;
        _gigaChat = gigaChat;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var bod = await _uow.BooksOfTheDay.Query()
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
            BookOfTheDay = bod?.Book != null ? _mapper.Map<BookCardViewModel>(bod.Book) : null,
            RecentBooks = recent.Select(_mapper.Map<BookCardViewModel>).ToList(),
            QuizQuestion = quiz
        };
        return View(vm);
    }

    [HttpGet("/quiz/next")]
    public async Task<IActionResult> QuizNext()
    {
        var q = await GetRandomQuizAsync();
        return Json(q);
    }

    [HttpPost("/quiz/answer")]
    public async Task<IActionResult> QuizAnswer([FromForm] int questionId, [FromForm] string answer)
    {
        var q = await _uow.QuizQuestions.GetByIdAsync(questionId);
        if (q == null) return NotFound();
        return Json(new QuizAnswerResultViewModel
        {
            IsCorrect = string.Equals(answer?.Trim(), q.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase),
            CorrectAnswer = q.CorrectAnswer,
            BookId = q.BookId
        });
    }

    [HttpPost("/ai/recommend")]
    public async Task<IActionResult> AiRecommend([FromForm] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new AiRecommendationViewModel { Query = "", Books = new() });

        var books = await _uow.Books.Query()
            .Where(b => !b.IsHidden && b.IsAvailable)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .ToListAsync();

        var bookList = books.Select(b => $"{b.Title} — {b.Author} ({b.Genre})").ToList();

        List<BookCardViewModel> result;
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
            }
            else
            {
                result = new();
            }
        }
        catch
        {
            result = new();
        }

        if (result.Count == 0)
        {
            var keywords = query.ToLowerInvariant()
                .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToArray();

            result = books
                .Select(b => new { book = b, score = ScoreBook(b, keywords) })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .Take(6)
                .Select(x => _mapper.Map<BookCardViewModel>(x.book))
                .ToList();
        }

        if (result.Count == 0)
            result = books.OrderBy(_ => Guid.NewGuid()).Take(6).Select(_mapper.Map<BookCardViewModel>).ToList();

        return Json(new AiRecommendationViewModel { Query = query, Books = result });
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

    static int ScoreBook(Book b, string[] keywords)
    {
        var haystack = $"{b.Title} {b.Author} {b.Description} {b.Genre}".ToLowerInvariant();
        return keywords.Count(k => haystack.Contains(k));
    }

    async Task<QuizQuestionViewModel?> GetRandomQuizAsync()
    {
        var count = await _uow.QuizQuestions.Query().CountAsync();
        if (count == 0) return null;
        var skip = Random.Shared.Next(count);
        var q = await _uow.QuizQuestions.Query().Skip(skip).Take(1).FirstAsync();
        var opts = new List<string> { q.CorrectAnswer, q.Option2, q.Option3, q.Option4 };
        opts = opts.OrderBy(_ => Random.Shared.Next()).ToList();
        return new QuizQuestionViewModel { Id = q.Id, Quote = q.Quote, Options = opts, BookId = q.BookId };
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/error")]
    public IActionResult Error() => View();
}
