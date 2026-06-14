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
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;

    public DiscussionController(IUnitOfWork uow, IMapper mapper, UserManager<User> userManager)
    {
        _uow = uow;
        _mapper = mapper;
        _userManager = userManager;
    }

    [HttpGet("Discussion/Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var discussion = await _uow.Discussions.Query()
            .Include(d => d.User)
            .Include(d => d.Messages).ThenInclude(m => m.User)
            .Include(d => d.Book)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (discussion == null) return NotFound();

        var vm = new DiscussionDetailsViewModel
        {
            Id = discussion.Id,
            BookId = discussion.BookId,
            BookTitle = discussion.Book?.Title ?? "",
            Title = discussion.Title,
            AuthorName = discussion.User?.UserName ?? "",
            Messages = discussion.Messages.OrderBy(m => m.CreatedAt).Select(m => new DiscussionMessageViewModel
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

        var userId = _userManager.GetUserId(User)!;
        var book = await _uow.Books.GetByIdAsync(bookId);
        if (book == null) return NotFound();

        var discussion = new Discussion
        {
            BookId = bookId,
            UserId = userId,
            Title = title.Trim()
        };
        await _uow.Discussions.AddAsync(discussion);
        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = discussion.Id });
    }
}
