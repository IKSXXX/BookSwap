using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Controllers;

public class UserController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UserController(UserManager<User> userManager, IUnitOfWork uow, IMapper mapper)
    {
        _userManager = userManager;
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet("User/{id}")]
    public async Task<IActionResult> Details(string id)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var currentId = _userManager.GetUserId(User);
        var isCurrent = currentId == user.Id;

        var myBooks = await _uow.Books.Query()
            .Where(b => !b.IsHidden && b.BookOwners.Any(bo => bo.UserId == user.Id))
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var favorites = await _uow.Favorites.Query()
            .Where(f => f.UserId == user.Id && !f.IsWishlist)
            .Include(f => f.Book!).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Select(f => f.Book!)
            .ToListAsync();

        var wishlist = await _uow.Favorites.Query()
            .Where(f => f.UserId == user.Id && f.IsWishlist)
            .Include(f => f.Book!).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Select(f => f.Book!)
            .ToListAsync();

        var exchanges = await _uow.Exchanges.Query()
            .Where(e => e.SenderId == user.Id || e.ReceiverId == user.Id)
            .Include(e => e.Sender).Include(e => e.Receiver)
            .Include(e => e.BookRequested)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();

        var reviewsReceived = await _uow.Reviews.Query()
            .Where(r => r.ToUserId == user.Id)
            .Include(r => r.FromUser).Include(r => r.ToUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var reviewsGiven = await _uow.Reviews.Query()
            .Where(r => r.FromUserId == user.Id)
            .Include(r => r.FromUser).Include(r => r.ToUser)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        ReviewDisplayViewModel MapReview(Review r) => new()
        {
            FromUserId = r.FromUserId,
            FromUserName = r.FromUser?.UserName ?? "",
            FromUserAvatar = r.FromUser?.AvatarPath,
            ToUserId = r.ToUserId,
            ToUserName = r.ToUser?.UserName ?? "",
            ToUserAvatar = r.ToUser?.AvatarPath,
            Rating = r.Rating ?? 0,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        };

        var vm = new UserProfileViewModel
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = isCurrent ? user.Email : null,
            AvatarPath = user.AvatarPath,
            Location = user.Location,
            Rating = user.Rating ?? 0,
            RegistrationDate = user.RegistrationDate,
            BooksCount = myBooks.Count,
            CompletedExchanges = exchanges.Count(e => e.Status == ExchangeStatus.Completed),
            IsCurrent = isCurrent,
            MyBooks = myBooks.Select(_mapper.Map<BookCardViewModel>).ToList(),
            Exchanges = exchanges.Select(e => ExchangeListItemViewModel.From(e, user.Id)).ToList(),
            Favorites = favorites.Select(_mapper.Map<BookCardViewModel>).ToList(),
            Wishlist = wishlist.Select(_mapper.Map<BookCardViewModel>).ToList(),
            ReviewsReceived = reviewsReceived.Select(MapReview).ToList(),
            ReviewsGiven = reviewsGiven.Select(MapReview).ToList(),
        };

        return View(vm);
    }
}
