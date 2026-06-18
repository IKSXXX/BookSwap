using AutoMapper;
using BookSwap.Db.Data;
using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.Data;
using BookSwap.Web.Helpers;
using BookSwap.Web.Services;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IAdminService _admin;

    public AdminController(IUnitOfWork uow, IMapper mapper, UserManager<User> userManager, IAdminService admin)
    {
        _uow = uow;
        _mapper = mapper;
        _userManager = userManager;
        _admin = admin;
    }

    async Task<List<BookCardViewModel>> AvailableBookCardsAsync()
    {
        var books = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Where(b => !b.IsHidden)
            .ToListAsync();
        return books.Select(_mapper.Map<BookCardViewModel>).ToList();
    }

    public async Task<IActionResult> Index()
    {
        var vm = new AdminStatsViewModel
        {
            UsersCount = _userManager.Users.Count(),
            BooksCount = await _uow.Books.Query().CountAsync(),
            ExchangesCount = await _uow.Exchanges.Query().CountAsync(),
            ReviewsCount = await _uow.Reviews.Query().CountAsync(),
            BlockedUsersCount = _userManager.Users.Count(u => u.IsBlocked),
            HiddenBooksCount = await _uow.Books.Query().CountAsync(b => b.IsHidden),
            PendingExchangesCount = await _uow.Exchanges.Query().CountAsync(e => e.Status == ExchangeStatus.Pending),
            CompletedExchangesCount = await _uow.Exchanges.Query().CountAsync(e => e.Status == ExchangeStatus.Completed),
            QuizQuestionsCount = await _uow.QuizQuestions.Query().CountAsync(),
            RecentUsers = _userManager.Users
                .OrderByDescending(u => u.RegistrationDate)
                .Take(5)
                .Select(u => new AdminRecentUserViewModel
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    RegistrationDate = u.RegistrationDate,
                    IsBlocked = u.IsBlocked
                })
                .ToList(),
            RecentBooks = await _uow.Books.Query()
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new AdminRecentBookViewModel
                {
                    Id = b.Id,
                    Title = b.Title,
                    Author = b.Author,
                    CreatedAt = b.CreatedAt,
                    IsHidden = b.IsHidden
                })
                .ToListAsync()
        };
        return View(vm);
    }

    public async Task<IActionResult> Users()
    {
        var users = _userManager.Users.ToList();
        var bookCounts = await _uow.BookOwners.Query()
            .GroupBy(bo => bo.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var list = new List<AdminUserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            list.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                IsBlocked = user.IsBlocked,
                IsAdmin = roles.Contains(DbSeeder.AdminRole),
                Rating = user.Rating ?? 0,
                RegistrationDate = user.RegistrationDate,
                BooksCount = bookCounts.GetValueOrDefault(user.Id)
            });
        }
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(string id)
    {
        var result = await _admin.ToggleUserBlockAsync(id);
        if (!result.Ok) return result.ToActionResult();
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var result = await _admin.ToggleUserAdminAsync(id);
        if (!result.Ok) return result.ToActionResult();
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> Books()
    {
        var books = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return View(books);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHidden(int id)
    {
        var result = await _admin.ToggleBookHiddenAsync(id);
        if (!result.Ok) return result.ToActionResult();
        return RedirectToAction(nameof(Books));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var result = await _admin.DeleteBookAsync(id);
        if (!result.Ok) return result.ToActionResult();
        return RedirectToAction(nameof(Books));
    }

    [HttpGet]
    public async Task<IActionResult> BookOfTheDay()
    {
        var today = DateTime.UtcNow.Date;
        var current = (await _uow.BooksOfTheDay.FindAsync(b => b.Date == today)).FirstOrDefault();
        var currentBook = current != null ? await _uow.Books.GetByIdAsync(current.BookId) : null;
        return View(new SetBookOfDayViewModel
        {
            Date = today,
            AvailableBooks = await AvailableBookCardsAsync(),
            CurrentBookOfDayId = current?.BookId,
            CurrentBookTitle = currentBook?.Title,
            CurrentBookAuthor = currentBook?.Author,
            CurrentBookCover = currentBook?.CoverImagePath
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BookOfTheDay(DateTime date, int bookId)
    {
        var result = await _admin.SetBookOfTheDayAsync(date, bookId);
        if (!result.Ok)
        {
            TempData["Error"] = result.Message;
            return RedirectToAction(nameof(BookOfTheDay));
        }
        TempData["Success"] = "Книга дня обновлена.";
        return RedirectToAction(nameof(BookOfTheDay));
    }

    public async Task<IActionResult> Quiz()
    {
        var questions = await _uow.QuizQuestions.Query().Include(q => q.Book).OrderBy(q => q.Id).ToListAsync();
        return View(questions);
    }

    [HttpGet]
    public async Task<IActionResult> CreateQuiz()
    {
        return View("QuizForm", new QuizQuestionFormViewModel
        {
            AvailableBooks = await AvailableBookCardsAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuiz(QuizQuestionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableBooks = await AvailableBookCardsAsync();
            return View("QuizForm", model);
        }
        await _admin.CreateQuizAsync(model);
        return RedirectToAction(nameof(Quiz));
    }

    [HttpGet]
    public async Task<IActionResult> EditQuiz(int id)
    {
        var question = await _uow.QuizQuestions.GetByIdAsync(id);
        if (question == null) return NotFound();
        return View("QuizForm", new QuizQuestionFormViewModel
        {
            Id = question.Id,
            BookId = question.BookId,
            Quote = question.Quote,
            CorrectAnswer = question.CorrectAnswer,
            Option2 = question.Option2,
            Option3 = question.Option3,
            Option4 = question.Option4,
            AvailableBooks = await AvailableBookCardsAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuiz(QuizQuestionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableBooks = await AvailableBookCardsAsync();
            return View("QuizForm", model);
        }
        var result = await _admin.EditQuizAsync(model);
        if (!result.Ok) return result.ToActionResult();
        TempData["Success"] = "Вопрос обновлён.";
        return RedirectToAction(nameof(Quiz));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        var result = await _admin.DeleteQuizAsync(id);
        if (!result.Ok) return result.ToActionResult();
        return RedirectToAction(nameof(Quiz));
    }
}
