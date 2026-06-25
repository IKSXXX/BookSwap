using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Web.Helpers;
using BookSwap.Db.Interfaces;
using BookSwap.Web.Services;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Controllers;

[Authorize]
public class ExchangeController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly UserManager<User> _userManager;
    private readonly IExchangeService _exchange;

    public ExchangeController(IUnitOfWork uow, IMapper mapper, UserManager<User> userManager, IExchangeService exchange)
    {
        _uow = uow;
        _mapper = mapper;
        _userManager = userManager;
        _exchange = exchange;
    }

    private string CurrentUserId => _userManager.GetUserId(User)!;

    [HttpGet]
    public async Task<IActionResult> Index(string? status)
    {
        var userId = CurrentUserId;
        var query = _uow.Exchanges.Query()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .Include(e => e.BookRequested).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Include(e => e.BookOffered).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Where(e => e.SenderId == userId || e.ReceiverId == userId);

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ExchangeStatus>(status, true, out var parsedStatus))
            query = query.Where(e => e.Status == parsedStatus);

        var exchanges = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();

        var items = exchanges.Select(e => ExchangeListItemViewModel.From(e, userId)).ToList();

        ViewBag.CurrentStatus = status ?? "all";
        return View(items);
    }

    [HttpGet("Exchange/Create/{bookId:int}")]
    public async Task<IActionResult> Create(int bookId)
    {
        var book = await _uow.Books.Query()
            .Include(b => b.BookOwners).ThenInclude(bo => bo.User)
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book == null || !book.IsAvailable) return NotFound();

        var userId = CurrentUserId;
        if (book.BookOwners.Any(bo => bo.UserId == userId))
            return BadRequest("Нельзя обменяться с самим собой.");

        var myBooks = await _uow.Books.Query()
            .Where(b => b.IsAvailable && !b.IsHidden && b.BookOwners.Any(bo => bo.UserId == userId))
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
        var result = await _exchange.CreateAsync(bookId, CurrentUserId, selectedOfferedBookId,
            id => Url.Action(nameof(Details), new { id }));

        if (!result.Ok) return result.ToActionResult();
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = CurrentUserId;
        var exchange = await _uow.Exchanges.Query()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .Include(e => e.BookRequested).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Include(e => e.BookOffered).ThenInclude(b => b.BookOwners).ThenInclude(bo => bo.User)
            .Include(e => e.Messages)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (exchange == null) return NotFound();
        if (exchange.SenderId != userId && exchange.ReceiverId != userId) return Forbid();

        var vm = new ExchangeDetailsViewModel
        {
            Id = exchange.Id,
            Status = exchange.Status,
            StatusLabel = MappingProfile.StatusToLabel(exchange.Status),
            CreatedAt = exchange.CreatedAt,
            Sender = _mapper.Map<OwnerSummaryViewModel>(exchange.Sender),
            Receiver = _mapper.Map<OwnerSummaryViewModel>(exchange.Receiver),
            BookRequested = _mapper.Map<BookCardViewModel>(exchange.BookRequested),
            BookOffered = exchange.BookOffered != null ? _mapper.Map<BookCardViewModel>(exchange.BookOffered) : null,
            Messages = exchange.Messages.OrderBy(m => m.SentAt).Select(m => new ChatMessageViewModel
            {
                SenderId = m.SenderId,
                SenderName = m.SenderId == exchange.SenderId ? exchange.Sender?.UserName ?? "" : exchange.Receiver?.UserName ?? "",
                SenderAvatar = m.SenderId == exchange.SenderId ? exchange.Sender?.AvatarPath : exchange.Receiver?.AvatarPath,
                Text = m.Text,
                SentAt = m.SentAt
            }).ToList(),
            CanAccept = exchange.Status == ExchangeStatus.Pending && exchange.ReceiverId == userId,
            CanReject = exchange.Status == ExchangeStatus.Pending && exchange.ReceiverId == userId,
            CanCancel = exchange.Status == ExchangeStatus.Pending && exchange.SenderId == userId,
            CanComplete = exchange.Status == ExchangeStatus.Accepted && (exchange.SenderId == userId || exchange.ReceiverId == userId),
            CanLeaveReview = exchange.Status == ExchangeStatus.Completed && !await _uow.Reviews.AnyAsync(r => r.ExchangeRequestId == exchange.Id && r.FromUserId == userId),
            CurrentUserId = userId
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int id)
    {
        var result = await _exchange.AcceptAsync(id, CurrentUserId);
        return result.Ok ? RedirectToAction(nameof(Details), new { id }) : result.ToActionResult();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _exchange.RejectAsync(id, CurrentUserId);
        return result.Ok ? RedirectToAction(nameof(Details), new { id }) : result.ToActionResult();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _exchange.CancelAsync(id, CurrentUserId);
        return result.Ok ? RedirectToAction(nameof(Details), new { id }) : result.ToActionResult();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _exchange.CompleteAsync(id, CurrentUserId);
        return result.Ok ? RedirectToAction(nameof(Details), new { id }) : result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> LeaveReview(int id)
    {
        var userId = CurrentUserId;
        var exchange = await _uow.Exchanges.Query()
            .Include(e => e.Sender).Include(e => e.Receiver)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (exchange == null || exchange.Status != ExchangeStatus.Completed) return NotFound();
        if (exchange.SenderId != userId && exchange.ReceiverId != userId) return Forbid();

        if (await _uow.Reviews.AnyAsync(r => r.ExchangeRequestId == id && r.FromUserId == userId))
        {
            TempData["Error"] = "Отзыв уже оставлен.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var otherId = exchange.SenderId == userId ? exchange.ReceiverId : exchange.SenderId;
        var otherName = (exchange.SenderId == userId ? exchange.Receiver?.UserName : exchange.Sender?.UserName) ?? "";

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

        var result = await _exchange.LeaveReviewAsync(model, CurrentUserId);
        if (!result.Ok) return result.ToActionResult();

        TempData["Success"] = "Спасибо за отзыв!";
        return RedirectToAction(nameof(Details), new { id = model.ExchangeRequestId });
    }
}
