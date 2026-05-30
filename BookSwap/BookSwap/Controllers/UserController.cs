using AutoMapper;
using BookExchange.Web.Entities;
using BookExchange.Web.Interfaces;
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
            Rating = user.Rating,
            RegistrationDate = user.RegistrationDate,
            BooksCount = myBooks.Count,
            MyBooks = myBooks.Select(_mapper.Map<BookCardViewModel>).ToList(),
            Favorites = favorites.Select(_mapper.Map<BookCardViewModel>).ToList(),
            ReviewsReceived = reviewsReceived.Select(r => new ReviewDisplayViewModel
            {
                FromUserId = r.FromUserId,
                FromUserName = r.FromUser?.UserName ?? "",
                ToUserId = r.ToUserId,
                ToUserName = r.ToUser?.UserName ?? "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList(),
            ReviewsGiven = reviewsGiven.Select(r => new ReviewDisplayViewModel
            {
                FromUserId = r.FromUserId,
                FromUserName = r.FromUser?.UserName ?? "",
                ToUserId = r.ToUserId,
                ToUserName = r.ToUser?.UserName ?? "",
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList(),
            IsCurrentUser = isCurrent
        };

        return View(vm);
    }
}
