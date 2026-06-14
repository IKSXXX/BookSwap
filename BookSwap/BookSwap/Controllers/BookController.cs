using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Web.Helpers;
using BookSwap.Db.Interfaces;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Controllers;

public class BookController : Controller
{
    private const int PageSize = 24;

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _env;

    public BookController(IUnitOfWork uow, IMapper mapper, UserManager<User> userManager, IWebHostEnvironment env)
    {
        _uow = uow;
        _mapper = mapper;
        _userManager = userManager;
        _env = env;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    private Task<bool> IsOwnerAsync(int bookId, string userId)
        => _uow.BookOwners.AnyAsync(bo => bo.BookId == bookId && bo.UserId == userId);

    [HttpGet]
    [Route("Book")]
    [Route("Book/Index")]
    [Route("catalog")]
    public async Task<IActionResult> Index(
        string? search = null,
        string? genres = null,
        string? conditions = null,
        bool onlyAvailable = false,
        int page = 1)
    {
        page = page < 1 ? 1 : page;

        var query = _uow.Books.Query()
            .Where(b => !b.IsHidden)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(searchTerm) || b.Author.ToLower().Contains(searchTerm));
        }

        var selectedGenres = genres?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? new();
        if (selectedGenres.Count > 0)
            query = query.Where(b => selectedGenres.Contains(b.Genre));

        var selectedConditions = (conditions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
            .Select(x => Enum.TryParse<BookCondition>(x, out var c) ? (BookCondition?)c : null)
            .Where(x => x != null).Select(x => x!.Value).ToList();
        if (selectedConditions.Count > 0)
            query = query.Where(b => selectedConditions.Contains(b.Condition));

        if (onlyAvailable)
            query = query.Where(b => b.IsAvailable);

        var total = await query.CountAsync();

        var books = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        var allGenres = await _uow.Books.Query()
            .Where(b => !b.IsHidden)
            .Select(b => b.Genre).Distinct().OrderBy(g => g).ToListAsync();

        var vm = new BookCatalogViewModel
        {
            Books = books.Select(_mapper.Map<BookCardViewModel>).ToList(),
            Pagination = new PaginationInfo { Page = page, PageSize = PageSize, TotalItems = total },
            SearchQuery = search,
            AllGenres = allGenres,
            SelectedGenres = selectedGenres,
            SelectedConditions = selectedConditions,
            OnlyAvailable = onlyAvailable
        };
        return View(vm);
    }

    [HttpGet]
    [Route("Book/Details/{id:int}")]
    [Route("book/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var book = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .FirstOrDefaultAsync(b => b.Id == id && !b.IsHidden);
        if (book == null) return NotFound();

        var similar = await _uow.Books.Query()
            .Where(b => b.Id != id && !b.IsHidden && (b.Genre == book.Genre || b.Author == book.Author))
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Take(4)
            .ToListAsync();

        var discussions = await _uow.Discussions.Query()
            .Where(d => d.BookId == id)
            .Include(d => d.User)
            .Include(d => d.Messages)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        var currentId = _userManager.GetUserId(User);
        var favorites = currentId == null
            ? new List<Favorite>()
            : (await _uow.Favorites.FindAsync(f => f.UserId == currentId && f.BookId == id)).ToList();
        var isFav = favorites.Any(f => !f.IsWishlist);
        var isWish = favorites.Any(f => f.IsWishlist);

        var vm = _mapper.Map<BookDetailsViewModel>(book);
        vm.Similar = similar.Select(_mapper.Map<BookCardViewModel>).ToList();
        vm.Discussions = discussions.Select(d => new DiscussionListItemViewModel
        {
            Id = d.Id,
            Title = d.Title,
            AuthorName = d.User?.UserName ?? "",
            MessagesCount = d.Messages.Count,
            CreatedAt = d.CreatedAt
        }).ToList();
        vm.IsFavorite = isFav;
        vm.IsInWishlist = isWish;

        return View(vm);
    }

    [Authorize, HttpGet]
    public IActionResult Create() => View("Form", new BookFormViewModel());

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var userId = CurrentUserId;
        var book = _mapper.Map<Book>(model);

        var path = await ImageHelper.SaveAsync(model.CoverImage, _env, "images/books");
        if (path != null) book.CoverImagePath = path;

        await _uow.Books.AddAsync(book);
        await _uow.SaveChangesAsync();

        await _uow.BookOwners.AddAsync(new BookOwner { BookId = book.Id, UserId = userId, IsPrimary = true });
        await _uow.SaveChangesAsync();

        TempData["Success"] = "Книга добавлена.";
        return RedirectToAction("Details", "User", new { id = userId });
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = CurrentUserId;
        var book = await _uow.Books.GetByIdAsync(id);
        if (book == null) return Forbid();
        if (!await IsOwnerAsync(id, userId)) return Forbid();

        return View("Form", new BookFormViewModel
        {
            Id = book.Id, Title = book.Title, Author = book.Author,
            ISBN = book.ISBN, Description = book.Description,
            Genre = book.Genre, Condition = book.Condition,
            Year = book.Year, Language = book.Language,
            ExistingCoverPath = book.CoverImagePath
        });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BookFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var userId = CurrentUserId;
        if (model.Id == null) return BadRequest();
        var book = await _uow.Books.GetByIdAsync(model.Id.Value);
        if (book == null) return Forbid();
        if (!await IsOwnerAsync(model.Id.Value, userId)) return Forbid();

        book.Title = model.Title;
        book.Author = model.Author;
        book.ISBN = model.ISBN;
        book.Description = model.Description;
        book.Genre = model.Genre;
        book.Condition = model.Condition;
        book.Year = model.Year;
        book.Language = model.Language;

        var path = await ImageHelper.SaveAsync(model.CoverImage, _env, "images/books");
        if (path != null) book.CoverImagePath = path;

        _uow.Books.Update(book);
        await _uow.SaveChangesAsync();

        TempData["Success"] = "Книга обновлена.";
        return RedirectToAction("Details", new { id = book.Id });
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = CurrentUserId;
        var book = await _uow.Books.GetByIdAsync(id);
        if (book == null) return Forbid();
        if (!await IsOwnerAsync(id, userId)) return Forbid();

        _uow.Books.Remove(book);
        await _uow.SaveChangesAsync();
        return RedirectToAction("Details", "User", new { id = userId });
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> FetchByIsbn(string isbn)
    {
        var result = await GoogleBooksHelper.FetchByISBNAsync(isbn);
        if (result == null) return Json(new { found = false });
        return Json(new { found = true, result.Title, result.Author, result.Description, Cover = result.CoverImageUrl, result.Year });
    }
}
