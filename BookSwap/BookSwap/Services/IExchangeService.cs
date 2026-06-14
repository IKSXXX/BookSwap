using BookSwap.Web.ViewModels;

namespace BookSwap.Web.Services;

public interface IExchangeService
{
    Task<ServiceResult<int>> CreateAsync(int bookId, string userId, int? offeredBookId, Func<int, string?> detailsUrl);
    Task<ServiceResult> AcceptAsync(int id, string userId);
    Task<ServiceResult> RejectAsync(int id, string userId);
    Task<ServiceResult> CancelAsync(int id, string userId);
    Task<ServiceResult> CompleteAsync(int id, string userId);
    Task<ServiceResult> LeaveReviewAsync(ReviewFormViewModel model, string userId);
}
