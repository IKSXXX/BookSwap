using AutoMapper;
using BookExchange.Web.Helpers;
using BookExchange.Web.Interfaces;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public HomeController(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var today = DateTime.Now.Date;
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

    private async Task<QuizQuestionViewModel?> GetRandomQuizAsync()
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

    [HttpPost("/ai/recommend")]
    public async Task<IActionResult> AiRecommend([FromForm] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Json(new AiRecommendationViewModel { Query = "", Books = new() });

        var books = await _uow.Books.Query()
            .Where(b => !b.IsHidden && b.IsAvailable)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .ToListAsync();

        var result = AIHelper.GetRecommendations(query, books)
            .Select(_mapper.Map<BookCardViewModel>)
            .ToList();

        return Json(new AiRecommendationViewModel { Query = query, Books = result });
    }

    [HttpGet("/error")]
    public IActionResult Error() => View();
}
