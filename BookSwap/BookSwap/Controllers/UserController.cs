using AutoMapper;
using BookExchange.Db.Entities;
using BookExchange.Web.Helpers;
using BookExchange.Db.Interfaces;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

public class UserController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UserController(UserManager<User> um, IUnitOfWork uow, IMapper mapper)
    {
        _userManager = um;
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

        var myBookIds = await _uow.BookOwners.Query()
            .Where(bo => bo.UserId == user.Id)
            .Select(bo => bo.BookId)
            .ToListAsync();

        var myBooks = await _uow.Books.Query()
            .Where(b => myBookIds.Contains(b.Id) && !b.IsHidden)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var favorites = await _uow.Favorites.Query()
            .Where(f => f.UserId == user.Id)
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
            Exchanges = exchanges.Select(e => new ExchangeListItemViewModel
            {
                Id = e.Id,
                Status = e.Status,
                StatusLabel = MappingProfile.StatusToLabel(e.Status),
                CreatedAt = e.CreatedAt,
                IsSender = e.SenderId == user.Id,
                OtherUserId = (e.SenderId == user.Id ? e.ReceiverId : e.SenderId) ?? "",
                OtherUserName = (e.SenderId == user.Id ? e.Receiver?.UserName : e.Sender?.UserName) ?? "",
                OtherUserAvatar = (e.SenderId == user.Id ? e.Receiver?.AvatarPath : e.Sender?.AvatarPath) ?? "",
                BookRequestedTitle = e.BookRequested?.Title ?? "",
                BookRequestedCover = e.BookRequested?.CoverImagePath,
                BookOfferedTitle = e.BookOffered?.Title,
                BookOfferedCover = e.BookOffered?.CoverImagePath
            }).ToList(),
            Favorites = favorites.Select(_mapper.Map<BookCardViewModel>).ToList(),
            ReviewsReceived = reviewsReceived.Select(r => new ReviewDisplayViewModel
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
            }).ToList(),
            ReviewsGiven = reviewsGiven.Select(r => new ReviewDisplayViewModel
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
            }).ToList(),
        };

        return View(vm);
    }
}
