using AutoMapper;
using BookExchange.Web.Interfaces;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public HomeController(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var recent = await _uow.Books.Query()
            .Where(b => !b.IsHidden)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(6)
            .ToListAsync();

        var vm = recent.Select(_mapper.Map<BookCardViewModel>).ToList();
        return View(vm);
    }

    [HttpGet("/privacy")]
    public IActionResult Privacy() => View();

    [HttpGet("/error")]
    public IActionResult Error() => View();
}
