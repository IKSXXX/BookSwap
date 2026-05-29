using BookExchange.Web.Data;
using BookExchange.Web.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _uow;

    public HomeController(IUnitOfWork uow) => _uow = uow;

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var recent = await _uow.Books.Query()
            .Where(b => !b.IsHidden)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(6)
            .ToListAsync();

        return View(recent);
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/error")]
    public IActionResult Error() => View();
}
