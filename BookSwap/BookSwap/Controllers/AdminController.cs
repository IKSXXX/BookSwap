using AutoMapper;
using BookExchange.Db.Data;
using BookExchange.Db.Entities;
using BookExchange.Db.Interfaces;
using BookExchange.Web.Data;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    readonly IUnitOfWork _uow;
    readonly IMapper _mapper;
    readonly UserManager<User> _um;

    public AdminController(IUnitOfWork uow, IMapper mapper, UserManager<User> um)
    {
        _uow = uow;
        _mapper = mapper;
        _um = um;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new AdminStatsViewModel
        {
            UsersCount = _um.Users.Count(),
            BooksCount = await _uow.Books.Query().CountAsync(),
            ExchangesCount = await _uow.Exchanges.Query().CountAsync(),
            ReviewsCount = await _uow.Reviews.Query().CountAsync(),
            BlockedUsersCount = _um.Users.Count(u => u.IsBlocked),
            HiddenBooksCount = await _uow.Books.Query().CountAsync(b => b.IsHidden),
            PendingExchangesCount = await _uow.Exchanges.Query().CountAsync(e => e.Status == ExchangeStatus.Pending),
            CompletedExchangesCount = await _uow.Exchanges.Query().CountAsync(e => e.Status == ExchangeStatus.Completed),
            QuizQuestionsCount = await _uow.QuizQuestions.Query().CountAsync(),
            RecentUsers = _um.Users
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
        var users = _um.Users.ToList();
        var list = new List<AdminUserListItemViewModel>();
        foreach (var u in users)
        {
            var roles = await _um.GetRolesAsync(u);
            list.Add(new AdminUserListItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? "",
                Email = u.Email ?? "",
                IsBlocked = u.IsBlocked,
                IsAdmin = roles.Contains(DbSeeder.AdminRole),
                Rating = u.Rating ?? 0,
                RegistrationDate = u.RegistrationDate,
                BooksCount = await _uow.BookOwners.Query().CountAsync(bo => bo.UserId == u.Id)
            });
        }
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(string id)
    {
        var user = await _um.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.IsBlocked = !user.IsBlocked;
        await _um.UpdateAsync(user);
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAdmin(string id)
    {
        var user = await _um.FindByIdAsync(id);
        if (user == null) return NotFound();
        if (await _um.IsInRoleAsync(user, DbSeeder.AdminRole))
            await _um.RemoveFromRoleAsync(user, DbSeeder.AdminRole);
        else
            await _um.AddToRoleAsync(user, DbSeeder.AdminRole);
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
        var book = await _uow.Books.GetByIdAsync(id);
        if (book == null) return NotFound();
        book.IsHidden = !book.IsHidden;
        _uow.Books.Update(book);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Books));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteBook(int id)
    {
        var book = await _uow.Books.GetByIdAsync(id);
        if (book == null) return NotFound();
        _uow.Books.Remove(book);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Books));
    }

    [HttpGet]
    public async Task<IActionResult> BookOfTheDay()
    {
        var books = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Where(b => !b.IsHidden)
            .ToListAsync();
        var today = DateTime.UtcNow.Date;
        var current = (await _uow.BooksOfTheDay.FindAsync(b => b.Date == today)).FirstOrDefault();
        var currentBook = current != null ? books.FirstOrDefault(b => b.Id == current.BookId) : null;
        return View(new SetBookOfDayViewModel
        {
            Date = today,
            AvailableBooks = books.Select(_mapper.Map<BookCardViewModel>).ToList(),
            CurrentBookOfDayId = current?.BookId,
            CurrentBookTitle = currentBook?.Title,
            CurrentBookAuthor = currentBook?.Author,
            CurrentBookCover = currentBook?.CoverImagePath,
            CurrentBookGenre = currentBook?.Genre
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BookOfTheDay(DateTime date, int bookId)
    {
        var day = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var existing = (await _uow.BooksOfTheDay.FindAsync(b => b.Date == day)).FirstOrDefault();
        if (existing != null)
        {
            existing.BookId = bookId;
            _uow.BooksOfTheDay.Update(existing);
        }
        else
        {
            await _uow.BooksOfTheDay.AddAsync(new BookOfTheDay { BookId = bookId, Date = day });
        }
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Книга дня обновлена.";
        return RedirectToAction(nameof(BookOfTheDay));
    }

    public async Task<IActionResult> Quiz()
    {
        var qs = await _uow.QuizQuestions.Query().Include(q => q.Book).OrderBy(q => q.Id).ToListAsync();
        return View(qs);
    }

    [HttpGet]
    public async Task<IActionResult> CreateQuiz()
    {
        var books = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Where(b => !b.IsHidden)
            .ToListAsync();
        return View("QuizForm", new QuizQuestionFormViewModel
        {
            AvailableBooks = books.Select(_mapper.Map<BookCardViewModel>).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateQuiz(QuizQuestionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
        var books = await _uow.Books.Query()
                .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Where(b => !b.IsHidden)
            .ToListAsync();
            model.AvailableBooks = books.Select(_mapper.Map<BookCardViewModel>).ToList();
            return View("QuizForm", model);
        }
        await _uow.QuizQuestions.AddAsync(new QuizQuestion
        {
            BookId = model.BookId,
            Quote = model.Quote,
            CorrectAnswer = model.CorrectAnswer,
            Option2 = model.Option2,
            Option3 = model.Option3,
            Option4 = model.Option4
        });
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Quiz));
    }

    [HttpGet]
    public async Task<IActionResult> EditQuiz(int id)
    {
        var q = await _uow.QuizQuestions.GetByIdAsync(id);
        if (q == null) return NotFound();
        var books = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Where(b => !b.IsHidden)
            .ToListAsync();
        return View("QuizForm", new QuizQuestionFormViewModel
        {
            Id = q.Id,
            BookId = q.BookId,
            Quote = q.Quote,
            CorrectAnswer = q.CorrectAnswer,
            Option2 = q.Option2,
            Option3 = q.Option3,
            Option4 = q.Option4,
            AvailableBooks = books.Select(_mapper.Map<BookCardViewModel>).ToList()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditQuiz(QuizQuestionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
        var books = await _uow.Books.Query()
                .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
                .Where(b => !b.IsHidden)
                .ToListAsync();
            model.AvailableBooks = books.Select(_mapper.Map<BookCardViewModel>).ToList();
            return View("QuizForm", model);
        }
        var q = await _uow.QuizQuestions.GetByIdAsync(model.Id ?? 0);
        if (q == null) return NotFound();
        q.BookId = model.BookId;
        q.Quote = model.Quote;
        q.CorrectAnswer = model.CorrectAnswer;
        q.Option2 = model.Option2;
        q.Option3 = model.Option3;
        q.Option4 = model.Option4;
        _uow.QuizQuestions.Update(q);
        await _uow.SaveChangesAsync();
        TempData["Success"] = "Вопрос обновлён.";
        return RedirectToAction(nameof(Quiz));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        var q = await _uow.QuizQuestions.GetByIdAsync(id);
        if (q == null) return NotFound();
        _uow.QuizQuestions.Remove(q);
        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Quiz));
    }
}
