using BookSwap.Web.ViewModels;

namespace BookSwap.Web.Services;

public interface IAdminService
{
    Task<ServiceResult> ToggleUserBlockAsync(string userId);
    Task<ServiceResult> ToggleUserAdminAsync(string userId);
    Task<ServiceResult> ToggleBookHiddenAsync(int bookId);
    Task<ServiceResult> DeleteBookAsync(int bookId);
    Task<ServiceResult> SetBookOfTheDayAsync(DateTime date, int bookId);
    Task<ServiceResult> CreateQuizAsync(QuizQuestionFormViewModel model);
    Task<ServiceResult> EditQuizAsync(QuizQuestionFormViewModel model);
    Task<ServiceResult> DeleteQuizAsync(int questionId);
}
