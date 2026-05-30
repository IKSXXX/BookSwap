using AutoMapper;
using BookExchange.Web.Entities;
using BookExchange.Web.Interfaces;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

public class BookController : Controller
{
    private const int PageSize = 24;

    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public BookController(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

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

        var q = _uow.Books.Query()
            .Where(b => !b.IsHidden)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(b => b.Title.ToLower().Contains(s) || b.Author.ToLower().Contains(s));
        }

        var selectedGenres = genres?.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim()).Where(x => x.Length > 0).ToList() ?? new();
        if (selectedGenres.Count > 0)
            q = q.Where(b => selectedGenres.Contains(b.Genre));

        var selectedConditions = (conditions?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>())
            .Select(x => Enum.TryParse<BookCondition>(x, out var c) ? (BookCondition?)c : null)
            .Where(x => x != null).Select(x => x!.Value).ToList();
        if (selectedConditions.Count > 0)
            q = q.Where(b => selectedConditions.Contains(b.Condition));

        if (onlyAvailable)
            q = q.Where(b => b.IsAvailable);

        var total = await q.CountAsync();

        var books = await q
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
}
