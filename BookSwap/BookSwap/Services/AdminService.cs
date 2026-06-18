using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.Data;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace BookSwap.Web.Services;

public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<User> _userManager;

    public AdminService(IUnitOfWork unitOfWork, UserManager<User> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<ServiceResult> ToggleUserBlockAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Fail(ServiceError.NotFound);

        user.IsBlocked = !user.IsBlocked;
        await _userManager.UpdateAsync(user);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ToggleUserAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return ServiceResult.Fail(ServiceError.NotFound);

        if (await _userManager.IsInRoleAsync(user, DbSeeder.AdminRole))
            await _userManager.RemoveFromRoleAsync(user, DbSeeder.AdminRole);
        else
            await _userManager.AddToRoleAsync(user, DbSeeder.AdminRole);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ToggleBookHiddenAsync(int bookId)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(bookId);
        if (book == null) return ServiceResult.Fail(ServiceError.NotFound);

        book.IsHidden = !book.IsHidden;
        _unitOfWork.Books.Update(book);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteBookAsync(int bookId)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(bookId);
        if (book == null) return ServiceResult.Fail(ServiceError.NotFound);

        _unitOfWork.Books.Remove(book);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetBookOfTheDayAsync(DateTime date, int bookId)
    {
        if (bookId <= 0)
            return ServiceResult.Fail(ServiceError.Invalid, "Выберите книгу перед сохранением.");

        var book = await _unitOfWork.Books.GetByIdAsync(bookId);
        if (book == null || book.IsHidden)
            return ServiceResult.Fail(ServiceError.Invalid, "Указанная книга не найдена или скрыта.");

        var day = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
        var existing = (await _unitOfWork.BooksOfTheDay.FindAsync(b => b.Date == day)).FirstOrDefault();
        if (existing != null)
        {
            existing.BookId = bookId;
            _unitOfWork.BooksOfTheDay.Update(existing);
        }
        else
        {
            await _unitOfWork.BooksOfTheDay.AddAsync(new BookOfTheDay { BookId = bookId, Date = day });
        }
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> CreateQuizAsync(QuizQuestionFormViewModel model)
    {
        await _unitOfWork.QuizQuestions.AddAsync(new QuizQuestion
        {
            BookId = model.BookId,
            Quote = model.Quote,
            CorrectAnswer = model.CorrectAnswer,
            Option2 = model.Option2,
            Option3 = model.Option3,
            Option4 = model.Option4
        });
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> EditQuizAsync(QuizQuestionFormViewModel model)
    {
        var question = await _unitOfWork.QuizQuestions.GetByIdAsync(model.Id ?? 0);
        if (question == null) return ServiceResult.Fail(ServiceError.NotFound);

        question.BookId = model.BookId;
        question.Quote = model.Quote;
        question.CorrectAnswer = model.CorrectAnswer;
        question.Option2 = model.Option2;
        question.Option3 = model.Option3;
        question.Option4 = model.Option4;
        _unitOfWork.QuizQuestions.Update(question);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteQuizAsync(int questionId)
    {
        var question = await _unitOfWork.QuizQuestions.GetByIdAsync(questionId);
        if (question == null) return ServiceResult.Fail(ServiceError.NotFound);

        _unitOfWork.QuizQuestions.Remove(question);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Success();
    }
}
