using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Controllers;

public class DiscussionController : Controller
{
    readonly IUnitOfWork _uow;
    readonly IMapper _mapper;
    readonly UserManager<User> _um;

    public DiscussionController(IUnitOfWork uow, IMapper mapper, UserManager<User> um)
    {
        _uow = uow;
        _mapper = mapper;
        _um = um;
    }

    [HttpGet("Discussion/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var disc = await _uow.Discussions.Query()
            .Include(d => d.User)
            .Include(d => d.Messages).ThenInclude(m => m.User)
            .Include(d => d.Book)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (disc == null) return NotFound();

        var vm = new DiscussionDetailsViewModel
        {
            Id = disc.Id,
            BookId = disc.BookId,
            BookTitle = disc.Book?.Title ?? "",
            Title = disc.Title,
            AuthorName = disc.User?.UserName ?? "",
            Messages = disc.Messages.OrderBy(m => m.CreatedAt).Select(m => new DiscussionMessageViewModel
            {
                UserName = m.User?.UserName ?? "",
                AvatarPath = m.User?.AvatarPath,
                Text = m.Text,
                CreatedAt = m.CreatedAt
            }).ToList()
        };

        return View(vm);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int bookId, string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 300)
        {
            TempData["Error"] = "Введите название темы (до 300 символов).";
            return RedirectToAction("Details", "Book", new { id = bookId });
        }

        var userId = _um.GetUserId(User)!;
        var book = await _uow.Books.GetByIdAsync(bookId);
        if (book == null) return NotFound();

        var d = new Discussion
        {
            BookId = bookId,
            UserId = userId,
            Title = title.Trim()
        };
        await _uow.Discussions.AddAsync(d);
        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = d.Id });
    }
}
