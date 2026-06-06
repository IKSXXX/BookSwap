using AutoMapper;
using BookExchange.Db.Entities;
using BookExchange.Web.Helpers;
using BookExchange.Db.Interfaces;
using BookExchange.Web.Hubs;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Controllers;

[Authorize]
public class ExchangeController : Controller
{
    readonly IUnitOfWork _uow;
    readonly IMapper _mapper;
    readonly UserManager<User> _um;
    readonly IHubContext<ChatHub> _hub;

    public ExchangeController(IUnitOfWork uow, IMapper mapper, UserManager<User> um, IHubContext<ChatHub> hub)
    {
        _uow = uow;
        _mapper = mapper;
        _um = um;
        _hub = hub;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        var userId = _um.GetUserId(User)!;
        var query = _uow.Exchanges.Query()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .Include(e => e.BookRequested).Include(e => e.BookOffered)
            .Where(e => e.SenderId == userId || e.ReceiverId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ExchangeStatus>(status, true, out var st))
            query = query.Where(e => e.Status == st);

        var exchanges = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

        var vms = exchanges.Select(e => new ExchangeListItemViewModel
        {
            Id = e.Id,
            Status = e.Status,
            StatusLabel = MappingProfile.StatusToLabel(e.Status),
            CreatedAt = e.CreatedAt,
            IsSender = e.SenderId == userId,
            OtherUserId = (e.SenderId == userId ? e.ReceiverId : e.SenderId) ?? "",
            OtherUserName = (e.SenderId == userId ? e.Receiver?.UserName : e.Sender?.UserName) ?? "",
            OtherUserAvatar = (e.SenderId == userId ? e.Receiver?.AvatarPath : e.Sender?.AvatarPath) ?? "",
            BookRequestedTitle = e.BookRequested?.Title ?? "",
            BookRequestedCover = e.BookRequested?.CoverImagePath,
            BookOfferedTitle = e.BookOffered?.Title,
            BookOfferedCover = e.BookOffered?.CoverImagePath
        }).ToList();

        ViewBag.CurrentStatus = status ?? "all";
        return View(vms);
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
        var book = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .FirstOrDefaultAsync(b => b.Id == bookId);
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

        // Notification for the receiver
        var sender = await _um.GetUserAsync(User);
        var notif = new Notification
        {
            UserId = receiverId,
            Type = "exchange",
            Text = $"{sender?.UserName ?? "Пользователь"} хочет обменяться с вами книгой «{book.Title}»",
            RelatedUrl = Url.Action(nameof(Details), new { id = request.Id })
        };
        await _uow.Notifications.AddAsync(notif);
        await _uow.SaveChangesAsync();

        try
        {
            await _hub.Clients.User(receiverId).SendAsync("ReceiveNotification", new
            {
                id = notif.Id,
                text = notif.Text,
                type = notif.Type,
                url = notif.RelatedUrl
            });
        }
        catch { /* SignalR push best-effort */ }

        return RedirectToAction(nameof(Details), new { id = request.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = _um.GetUserId(User)!;
        var ex = await _uow.Exchanges.GetByIdAsync(id);
        if (ex == null) return NotFound();
        if (ex.SenderId != userId && ex.ReceiverId != userId) return Forbid();

        var sender = await _um.FindByIdAsync(ex.SenderId);
        var receiver = await _um.FindByIdAsync(ex.ReceiverId);
        var messages = await _uow.Messages.FindAsync(m => m.ExchangeRequestId == id);

        var vm = new ExchangeDetailsViewModel
        {
            Id = ex.Id,
            Status = ex.Status,
            StatusLabel = MappingProfile.StatusToLabel(ex.Status),
            CreatedAt = ex.CreatedAt,
            Sender = _mapper.Map<OwnerSummaryViewModel>(sender),
            Receiver = _mapper.Map<OwnerSummaryViewModel>(receiver),
            BookRequested = _mapper.Map<BookCardViewModel>(await _uow.Books.GetByIdAsync(ex.BookRequestedId)),
            BookOffered = ex.BookOfferedId.HasValue ? _mapper.Map<BookCardViewModel>(await _uow.Books.GetByIdAsync(ex.BookOfferedId.Value)) : null,
            Messages = messages.OrderBy(m => m.SentAt).Select(m => new ChatMessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = m.SenderId == ex.SenderId ? sender?.UserName ?? "" : receiver?.UserName ?? "",
                SenderAvatar = m.SenderId == ex.SenderId ? sender?.AvatarPath : receiver?.AvatarPath,
                Text = m.Text,
                SentAt = m.SentAt
            }).ToList(),
            CanAccept = ex.Status == ExchangeStatus.Pending && ex.ReceiverId == userId,
            CanReject = ex.Status == ExchangeStatus.Pending && ex.ReceiverId == userId,
            CanCancel = ex.Status == ExchangeStatus.Pending && ex.SenderId == userId,
            CanComplete = ex.Status == ExchangeStatus.Accepted && (ex.SenderId == userId || ex.ReceiverId == userId),
            CanLeaveReview = ex.Status == ExchangeStatus.Completed && !await _uow.Reviews.AnyAsync(r => r.ExchangeRequestId == ex.Id && r.FromUserId == userId),
            CurrentUserId = userId
        };

        return View(vm);
    }

    async Task<IActionResult> TransitionAsync(int id, ExchangeStatus from, ExchangeStatus to, bool receiverOnly = false, bool senderOnly = false, Func<ExchangeRequest, Task>? onAccept = null)
    {
        var userId = _um.GetUserId(User)!;
        var ex = await _uow.Exchanges.GetByIdAsync(id);
        if (ex == null || ex.Status != from) return NotFound();
        if (receiverOnly && ex.ReceiverId != userId) return Forbid();
        if (senderOnly && ex.SenderId != userId) return Forbid();
        if (!receiverOnly && !senderOnly && ex.SenderId != userId && ex.ReceiverId != userId) return Forbid();

        ex.Status = to;
        _uow.Exchanges.Update(ex);

        if (onAccept != null)
            await onAccept(ex);

        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Accept(int id)
        => TransitionAsync(id, ExchangeStatus.Pending, ExchangeStatus.Accepted, receiverOnly: true, onAccept: async ex =>
        {
            var reqBook = await _uow.Books.GetByIdAsync(ex.BookRequestedId);
            if (reqBook != null) { reqBook.IsAvailable = false; _uow.Books.Update(reqBook); }
            if (ex.BookOfferedId.HasValue)
            {
                var offBook = await _uow.Books.GetByIdAsync(ex.BookOfferedId.Value);
                if (offBook != null) { offBook.IsAvailable = false; _uow.Books.Update(offBook); }
            }
        });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(int id)
        => TransitionAsync(id, ExchangeStatus.Pending, ExchangeStatus.Rejected, receiverOnly: true);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(int id)
        => TransitionAsync(id, ExchangeStatus.Pending, ExchangeStatus.Cancelled, senderOnly: true);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id) => await TransitionAsync(id, ExchangeStatus.Accepted, ExchangeStatus.Completed, onAccept: async ex =>
    {
        await TransferBook(ex.BookRequestedId, ex.SenderId);
        if (ex.BookOfferedId.HasValue)
            await TransferBook(ex.BookOfferedId.Value, ex.ReceiverId);
    });

    async Task TransferBook(int bookId, string newOwnerId)
    {
        var book = await _uow.Books.GetByIdAsync(bookId);
        if (book == null) return;
        var old = (await _uow.BookOwners.FindAsync(o => o.BookId == bookId && o.IsPrimary)).FirstOrDefault();
        if (old != null) _uow.BookOwners.Remove(old);
        await _uow.BookOwners.AddAsync(new BookOwner { BookId = bookId, UserId = newOwnerId, IsPrimary = true });
        book.IsAvailable = true;
        _uow.Books.Update(book);
    }

    [HttpGet]
    public async Task<IActionResult> LeaveReview(int id)
    {
        var userId = _um.GetUserId(User)!;
        var ex = await _uow.Exchanges.Query()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ex == null || ex.Status != ExchangeStatus.Completed) return NotFound();
        if (ex.SenderId != userId && ex.ReceiverId != userId) return Forbid();

        if (await _uow.Reviews.AnyAsync(r => r.ExchangeRequestId == id && r.FromUserId == userId))
        {
            TempData["Error"] = "Отзыв уже оставлен.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var otherId = ex.SenderId == userId ? ex.ReceiverId : ex.SenderId;
        var otherName = (ex.SenderId == userId ? ex.Receiver?.UserName : ex.Sender?.UserName) ?? "";

        return View(new ReviewFormViewModel
        {
            ExchangeRequestId = id,
            ToUserId = otherId,
            ToUserName = otherName
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LeaveReview(ReviewFormViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = _um.GetUserId(User)!;
        var ex = await _uow.Exchanges.GetByIdAsync(model.ExchangeRequestId);
        if (ex == null || ex.Status != ExchangeStatus.Completed) return NotFound();
        if (ex.SenderId != userId && ex.ReceiverId != userId) return Forbid();

        if (await _uow.Reviews.AnyAsync(r => r.ExchangeRequestId == ex.Id && r.FromUserId == userId))
            return BadRequest();

        await _uow.Reviews.AddAsync(new Review
        {
            FromUserId = userId,
            ToUserId = model.ToUserId,
            ExchangeRequestId = ex.Id,
            Rating = model.Rating,
            Comment = model.Comment
        });
        await _uow.SaveChangesAsync();

        var avg = await _uow.Reviews.Query()
            .Where(r => r.ToUserId == model.ToUserId)
            .Select(r => r.Rating)
            .ToListAsync();
        var user = await _um.FindByIdAsync(model.ToUserId);
        if (user != null)
        {
            user.Rating = avg.Count > 0 ? Math.Round(avg.Average() ?? 0, 2) : 0;
            await _um.UpdateAsync(user);
        }

        TempData["Success"] = "Спасибо за отзыв!";
        return RedirectToAction(nameof(Details), new { id = ex.Id });
    }
}
