using BookExchange.Web.Entities;
using BookExchange.Web.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BookExchange.Web.Controllers;

[Authorize]
public class FavoriteController : Controller
{
    readonly IUnitOfWork _uow;
    readonly UserManager<User> _um;

    public FavoriteController(IUnitOfWork uow, UserManager<User> um)
    {
        _uow = uow;
        _um = um;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int bookId, bool isWishlist = false, string? returnUrl = null)
    {
        var userId = _um.GetUserId(User)!;
        var existing = (await _uow.Favorites.FindAsync(f => f.UserId == userId && f.BookId == bookId && f.IsWishlist == isWishlist)).FirstOrDefault();

        if (existing != null)
            _uow.Favorites.Remove(existing);
        else
            await _uow.Favorites.AddAsync(new Favorite { UserId = userId, BookId = bookId, IsWishlist = isWishlist });

        await _uow.SaveChangesAsync();
        return Redirect(returnUrl ?? Url.Action("Details", "Book", new { id = bookId })!);
    }
}
