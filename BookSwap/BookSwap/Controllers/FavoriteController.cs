using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookSwap.Web.Controllers;

[Authorize]
public class FavoriteController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<User> _userManager;

    public FavoriteController(IUnitOfWork uow, UserManager<User> userManager)
    {
        _uow = uow;
        _userManager = userManager;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int bookId, bool isWishlist = false, string? returnUrl = null)
    {
        var userId = _userManager.GetUserId(User)!;
        var existing = (await _uow.Favorites.FindAsync(f => f.UserId == userId && f.BookId == bookId && f.IsWishlist == isWishlist)).FirstOrDefault();

        if (existing != null)
            _uow.Favorites.Remove(existing);
        else
            await _uow.Favorites.AddAsync(new Favorite { UserId = userId, BookId = bookId, IsWishlist = isWishlist });

        await _uow.SaveChangesAsync();
        return Redirect(returnUrl ?? Url.Action("Details", "Book", new { id = bookId })!);
    }
}
