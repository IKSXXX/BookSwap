using BookExchange.Web.Data;
using BookExchange.Web.Entities;
using BookExchange.Web.Interfaces;
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
    readonly UserManager<User> _um;
    readonly RoleManager<IdentityRole> _rm;

    public AdminController(IUnitOfWork uow, UserManager<User> um, RoleManager<IdentityRole> rm)
    {
        _uow = uow;
        _um = um;
        _rm = rm;
    }

    public async Task<IActionResult> Index()
    {
        var vm = new AdminStatsViewModel
        {
            UsersCount = _um.Users.Count(),
            BooksCount = await _uow.Books.Query().CountAsync(),
            ExchangesCount = await _uow.Exchanges.Query().CountAsync(),
            ReviewsCount = await _uow.Reviews.Query().CountAsync()
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
                Rating = u.Rating,
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
}
