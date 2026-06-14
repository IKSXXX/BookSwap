using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Services;

public class ExchangeService : IExchangeService
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<User> _userManager;
    private readonly INotificationService _notifications;

    public ExchangeService(IUnitOfWork uow, UserManager<User> userManager, INotificationService notifications)
    {
        _uow = uow;
        _userManager = userManager;
        _notifications = notifications;
    }

    public async Task<ServiceResult<int>> CreateAsync(int bookId, string userId, int? offeredBookId, Func<int, string?> detailsUrl)
    {
        var book = await _uow.Books.Query()
            .Include(b => b.BookOwners)
            .FirstOrDefaultAsync(b => b.Id == bookId);

        if (book == null || !book.IsAvailable) return ServiceResult<int>.Fail(ServiceError.NotFound);

        if (book.BookOwners.Any(bo => bo.UserId == userId))
            return ServiceResult<int>.Fail(ServiceError.Invalid, "Нельзя обменяться с самим собой.");

        if (offeredBookId.HasValue)
        {
            var offered = await _uow.Books.Query()
                .Include(b => b.BookOwners)
                .FirstOrDefaultAsync(b => b.Id == offeredBookId.Value);
            if (offered == null || !offered.IsAvailable) return ServiceResult<int>.Fail(ServiceError.Invalid);
            if (!offered.BookOwners.Any(bo => bo.UserId == userId)) return ServiceResult<int>.Fail(ServiceError.Invalid);
        }

        var receiverId = book.PrimaryOwnerId;
        var request = new ExchangeRequest
        {
            BookRequestedId = book.Id,
            BookOfferedId = offeredBookId,
            SenderId = userId,
            ReceiverId = receiverId,
            Status = ExchangeStatus.Pending
        };
        await _uow.Exchanges.AddAsync(request);
        await _uow.SaveChangesAsync();

        var sender = await _userManager.FindByIdAsync(userId);
        await _notifications.NotifyAsync(receiverId, "exchange",
            $"{sender?.UserName ?? "Пользователь"} хочет обменяться с вами книгой «{book.Title}»",
            detailsUrl(request.Id));

        return ServiceResult<int>.Success(request.Id);
    }

    public Task<ServiceResult> AcceptAsync(int id, string userId)
        => TransitionAsync(id, userId, ExchangeStatus.Pending, ExchangeStatus.Accepted, receiverOnly: true, onTransition: async exchange =>
        {
            var requestedBook = await _uow.Books.GetByIdAsync(exchange.BookRequestedId);
            if (requestedBook != null) { requestedBook.IsAvailable = false; _uow.Books.Update(requestedBook); }
            if (exchange.BookOfferedId.HasValue)
            {
                var offeredBook = await _uow.Books.GetByIdAsync(exchange.BookOfferedId.Value);
                if (offeredBook != null) { offeredBook.IsAvailable = false; _uow.Books.Update(offeredBook); }
            }
        });

    public Task<ServiceResult> RejectAsync(int id, string userId)
        => TransitionAsync(id, userId, ExchangeStatus.Pending, ExchangeStatus.Rejected, receiverOnly: true);

    public Task<ServiceResult> CancelAsync(int id, string userId)
        => TransitionAsync(id, userId, ExchangeStatus.Pending, ExchangeStatus.Cancelled, senderOnly: true);

    public Task<ServiceResult> CompleteAsync(int id, string userId)
        => TransitionAsync(id, userId, ExchangeStatus.Accepted, ExchangeStatus.Completed, onTransition: async exchange =>
        {
            await TransferBook(exchange.BookRequestedId, exchange.SenderId);
            if (exchange.BookOfferedId.HasValue)
                await TransferBook(exchange.BookOfferedId.Value, exchange.ReceiverId);
        });

    public async Task<ServiceResult> LeaveReviewAsync(ReviewFormViewModel model, string userId)
    {
        var exchange = await _uow.Exchanges.GetByIdAsync(model.ExchangeRequestId);
        if (exchange == null || exchange.Status != ExchangeStatus.Completed) return ServiceResult.Fail(ServiceError.NotFound);
        if (exchange.SenderId != userId && exchange.ReceiverId != userId) return ServiceResult.Fail(ServiceError.Forbidden);

        if (await _uow.Reviews.AnyAsync(r => r.ExchangeRequestId == exchange.Id && r.FromUserId == userId))
            return ServiceResult.Fail(ServiceError.Invalid, "Отзыв уже оставлен.");

        await _uow.Reviews.AddAsync(new Review
        {
            FromUserId = userId,
            ToUserId = model.ToUserId,
            ExchangeRequestId = exchange.Id,
            Rating = model.Rating,
            Comment = model.Comment
        });
        await _uow.SaveChangesAsync();

        await RecalculateRatingAsync(model.ToUserId);
        return ServiceResult.Success();
    }

    async Task<ServiceResult> TransitionAsync(int id, string userId, ExchangeStatus from, ExchangeStatus to,
        bool receiverOnly = false, bool senderOnly = false, Func<ExchangeRequest, Task>? onTransition = null)
    {
        var exchange = await _uow.Exchanges.GetByIdAsync(id);
        if (exchange == null || exchange.Status != from) return ServiceResult.Fail(ServiceError.NotFound);
        if (receiverOnly && exchange.ReceiverId != userId) return ServiceResult.Fail(ServiceError.Forbidden);
        if (senderOnly && exchange.SenderId != userId) return ServiceResult.Fail(ServiceError.Forbidden);
        if (!receiverOnly && !senderOnly && exchange.SenderId != userId && exchange.ReceiverId != userId)
            return ServiceResult.Fail(ServiceError.Forbidden);

        exchange.Status = to;
        _uow.Exchanges.Update(exchange);

        if (onTransition != null)
            await onTransition(exchange);

        await _uow.SaveChangesAsync();
        return ServiceResult.Success();
    }

    async Task TransferBook(int bookId, string newOwnerId)
    {
        var book = await _uow.Books.GetByIdAsync(bookId);
        if (book == null) return;
        var previousOwner = (await _uow.BookOwners.FindAsync(o => o.BookId == bookId && o.IsPrimary)).FirstOrDefault();
        if (previousOwner != null) _uow.BookOwners.Remove(previousOwner);
        await _uow.BookOwners.AddAsync(new BookOwner { BookId = bookId, UserId = newOwnerId, IsPrimary = true });
        book.IsAvailable = true;
        _uow.Books.Update(book);
    }

    async Task RecalculateRatingAsync(string userId)
    {
        var ratings = await _uow.Reviews.Query()
            .Where(r => r.ToUserId == userId)
            .Select(r => r.Rating)
            .ToListAsync();
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.Rating = ratings.Count > 0 ? Math.Round(ratings.Average() ?? 0, 2) : 0;
            await _userManager.UpdateAsync(user);
        }
    }
}
