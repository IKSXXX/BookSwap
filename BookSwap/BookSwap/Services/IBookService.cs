using BookSwap.Web.ViewModels;

namespace BookSwap.Web.Services;

public interface IBookService
{
    Task<ServiceResult<int>> CreateAsync(BookFormViewModel model, string userId, string? coverPath);
    Task<ServiceResult<BookFormViewModel>> GetForEditAsync(int bookId, string userId);
    Task<ServiceResult> EditAsync(BookFormViewModel model, string userId, string? coverPath);
    Task<ServiceResult> DeleteAsync(int bookId, string userId);
}
