using AutoMapper;
using BookExchange.Web.Entities;
using BookExchange.Web.Helpers;
using BookExchange.Web.Interfaces;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

[Authorize]
public class ExchangeController : Controller
{
    readonly IUnitOfWork _uow;
    readonly IMapper _mapper;
    readonly UserManager<User> _um;

    public ExchangeController(IUnitOfWork uow, IMapper mapper, UserManager<User> um)
    {
        _uow = uow;
        _mapper = mapper;
        _um = um;
    }

    [HttpGet("Exchange/Create/{bookId:int}")]
    public async Task<IActionResult> Create(int bookId)
    {
        var book = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book == null || !book.IsAvailable) return NotFound();

        var userId = _um.GetUserId(User)!;
        var isOwner = await _uow.BookOwners.AnyAsync(bo => bo.BookId == bookId && bo.UserId == userId);
        if (isOwner) return BadRequest("Нельзя обменяться с самим собой.");

        var myBookIds = await _uow.BookOwners.Query()
            .Where(bo => bo.UserId == userId)
            .Select(bo => bo.BookId)
            .ToListAsync();

        var myBooks = await _uow.Books.Query()
            .Where(b => myBookIds.Contains(b.Id) && b.IsAvailable && !b.IsHidden)
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .ToListAsync();

        return View(new CreateExchangeViewModel
        {
            BookRequestedId = book.Id,
            BookRequested = _mapper.Map<BookCardViewModel>(book),
            MyAvailableBooks = myBooks.Select(_mapper.Map<BookCardViewModel>).ToList()
        });
    }

    [HttpPost("Exchange/Create/{bookId:int}"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int bookId, int? selectedOfferedBookId)
    {
        var book = await _uow.Books.GetByIdAsync(bookId);
        if (book == null || !book.IsAvailable) return NotFound();

        var userId = _um.GetUserId(User)!;
        var isOwner = await _uow.BookOwners.AnyAsync(bo => bo.BookId == bookId && bo.UserId == userId);
        if (isOwner) return BadRequest();

        Book? offered = null;
        if (selectedOfferedBookId.HasValue)
        {
            offered = await _uow.Books.GetByIdAsync(selectedOfferedBookId.Value);
            if (offered == null || !offered.IsAvailable) return BadRequest();
            var isOfferedOwner = await _uow.BookOwners.AnyAsync(bo => bo.BookId == selectedOfferedBookId.Value && bo.UserId == userId);
            if (!isOfferedOwner) return BadRequest();
        }

        var receiverId = book.PrimaryOwnerId;

        var request = new ExchangeRequest
        {
            BookRequestedId = book.Id,
            BookOfferedId = offered?.Id,
            SenderId = userId,
            ReceiverId = receiverId,
            Status = ExchangeStatus.Pending
        };
        await _uow.Exchanges.AddAsync(request);
        await _uow.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = request.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _um.GetUserId(User)!;
        var ex = await _uow.Exchanges.Query()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .Include(e => e.BookRequested!).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Include(e => e.BookOffered!).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Include(e => e.Messages).ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ex == null) return NotFound();
        if (ex.SenderId != userId && ex.ReceiverId != userId) return Forbid();

        var vm = new ExchangeDetailsViewModel
        {
            Id = ex.Id,
            Status = ex.Status,
            StatusLabel = MappingProfile.StatusToLabel(ex.Status),
            CreatedAt = ex.CreatedAt,
            Sender = new OwnerSummaryViewModel { Id = ex.SenderId, Name = ex.Sender?.UserName ?? "" },
            Receiver = new OwnerSummaryViewModel { Id = ex.ReceiverId, Name = ex.Receiver?.UserName ?? "" },
            BookRequested = _mapper.Map<BookCardViewModel>(ex.BookRequested!),
            BookOffered = ex.BookOffered != null ? _mapper.Map<BookCardViewModel>(ex.BookOffered) : null,
            Messages = ex.Messages.OrderBy(m => m.SentAt).Select(m => new ChatMessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = m.Sender?.UserName ?? "",
                Text = m.Text,
                SentAt = m.SentAt
            }).ToList(),
            CanAccept = ex.Status == ExchangeStatus.Pending && ex.ReceiverId == userId,
            CanReject = ex.Status == ExchangeStatus.Pending && ex.ReceiverId == userId,
            CanCancel = ex.Status == ExchangeStatus.Pending && ex.SenderId == userId,
            CanComplete = ex.Status == ExchangeStatus.Accepted,
            CurrentUserId = userId
        };

        return View(vm);
    }
}
