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
        var ex = await _uow.Exchanges.GetByIdAsync(id);

        if (ex == null) return NotFound();
        if (ex.SenderId != userId && ex.ReceiverId != userId) return Forbid();

        var sender = await _um.FindByIdAsync(ex.SenderId);
        var receiver = await _um.FindByIdAsync(ex.ReceiverId);
        var bookRequested = await _uow.Books.GetByIdAsync(ex.BookRequestedId);
        Book? bookOffered = ex.BookOfferedId.HasValue ? await _uow.Books.GetByIdAsync(ex.BookOfferedId.Value) : null;

        var messages = await _uow.Messages.FindAsync(m => m.ExchangeRequestId == id);
        var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
        var senders = new Dictionary<string, User>();
        foreach (var sid in senderIds)
        {
            var u = await _um.FindByIdAsync(sid);
            if (u != null) senders[sid] = u;
        }

        var vm = new ExchangeDetailsViewModel
        {
            Id = ex.Id,
            Status = ex.Status,
            StatusLabel = MappingProfile.StatusToLabel(ex.Status),
            CreatedAt = ex.CreatedAt,
            Sender = new OwnerSummaryViewModel { Id = ex.SenderId, Name = sender?.UserName ?? "" },
            Receiver = new OwnerSummaryViewModel { Id = ex.ReceiverId, Name = receiver?.UserName ?? "" },
            BookRequested = bookRequested != null ? _mapper.Map<BookCardViewModel>(bookRequested) : null,
            BookOffered = bookOffered != null ? _mapper.Map<BookCardViewModel>(bookOffered) : null,
            Messages = messages.OrderBy(m => m.SentAt).Select(m => new ChatMessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = senders.GetValueOrDefault(m.SenderId)?.UserName ?? "",
                Text = m.Text,
                SentAt = m.SentAt
            }).ToList(),
            CanAccept = ex.Status == ExchangeStatus.Pending && ex.ReceiverId == userId,
            CanReject = ex.Status == ExchangeStatus.Pending && ex.ReceiverId == userId,
            CanCancel = ex.Status == ExchangeStatus.Pending && ex.SenderId == userId,
            CanComplete = ex.Status == ExchangeStatus.Accepted,
            CanLeaveReview = ex.Status == ExchangeStatus.Completed && !await _uow.Reviews.AnyAsync(r => r.ExchangeRequestId == ex.Id && r.FromUserId == userId),
            CurrentUserId = userId
        };

        return View(vm);
    }

    async Task<IActionResult> TransitionAsync(int id, ExchangeStatus from, ExchangeStatus to, Func<ExchangeRequest, Task>? onAccept = null)
    {
        var userId = _um.GetUserId(User)!;
        var ex = await _uow.Exchanges.GetByIdAsync(id);
        if (ex == null || ex.Status != from) return NotFound();
        if (ex.SenderId != userId && ex.ReceiverId != userId) return Forbid();

        ex.Status = to;
        _uow.Exchanges.Update(ex);

        if (onAccept != null)
            await onAccept(ex);

        await _uow.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Accept(int id)
        => TransitionAsync(id, ExchangeStatus.Pending, ExchangeStatus.Accepted);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(int id)
        => TransitionAsync(id, ExchangeStatus.Pending, ExchangeStatus.Rejected);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(int id)
        => TransitionAsync(id, ExchangeStatus.Pending, ExchangeStatus.Cancelled);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id) => await TransitionAsync(id, ExchangeStatus.Accepted, ExchangeStatus.Completed, onAccept: async ex =>
    {
        var reqBook = await _uow.Books.GetByIdAsync(ex.BookRequestedId);
        if (reqBook != null)
        {
            var oldOwner = (await _uow.BookOwners.FindAsync(bo => bo.BookId == reqBook.Id && bo.IsPrimary)).FirstOrDefault();
            if (oldOwner != null) _uow.BookOwners.Remove(oldOwner);
            await _uow.BookOwners.AddAsync(new BookOwner { BookId = reqBook.Id, UserId = ex.SenderId, IsPrimary = true });
            reqBook.IsAvailable = true;
            _uow.Books.Update(reqBook);
        }
        if (ex.BookOfferedId.HasValue)
        {
            var offBook = await _uow.Books.GetByIdAsync(ex.BookOfferedId.Value);
            if (offBook != null)
            {
                var oldOwner = (await _uow.BookOwners.FindAsync(bo => bo.BookId == offBook.Id && bo.IsPrimary)).FirstOrDefault();
                if (oldOwner != null) _uow.BookOwners.Remove(oldOwner);
                await _uow.BookOwners.AddAsync(new BookOwner { BookId = offBook.Id, UserId = ex.ReceiverId, IsPrimary = true });
                offBook.IsAvailable = true;
                _uow.Books.Update(offBook);
            }
        }
    });

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
            user.Rating = avg.Count > 0 ? Math.Round(avg.Average(), 2) : 0;
            await _um.UpdateAsync(user);
        }

        TempData["Success"] = "Спасибо за отзыв!";
        return RedirectToAction(nameof(Details), new { id = ex.Id });
    }
}
