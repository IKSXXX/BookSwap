using BookSwap.Db.Entities;
using BookSwap.Tests.Testing;
using BookSwap.Web.Services;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace BookSwap.Tests;

public class AdminServiceTests
{
    private readonly TestUnitOfWork _uow = new();

    private AdminService BuildService(Mock<UserManager<User>>? userManager = null)
        => new(_uow, (userManager ?? TestDoubles.MockUserManager("admin")).Object);

    private static QuizQuestionFormViewModel QuizForm(int? id = null, int bookId = 1, string correct = "Ответ")
        => new() { Id = id, BookId = bookId, Quote = "Цитата", CorrectAnswer = correct, Option2 = "Б", Option3 = "В", Option4 = "Г" };

    // ---------- книги ----------

    [Fact]
    public async Task ToggleBookHidden_FlipsFlag()
    {
        var book = _uow.AddBook(id: 1, hidden: false);

        var result = await BuildService().ToggleBookHiddenAsync(1);

        Assert.True(result.Ok);
        Assert.True(book.IsHidden);
    }

    [Fact]
    public async Task ToggleBookHidden_MissingBook_ReturnsNotFound()
    {
        var result = await BuildService().ToggleBookHiddenAsync(999);

        Assert.Equal(ServiceError.NotFound, result.Error);
    }

    [Fact]
    public async Task DeleteBook_RemovesBook()
    {
        _uow.AddBook(id: 1);

        var result = await BuildService().DeleteBookAsync(1);

        Assert.True(result.Ok);
        Assert.Empty(_uow.Books);
    }

    [Fact]
    public async Task DeleteBook_MissingBook_ReturnsNotFound()
    {
        var result = await BuildService().DeleteBookAsync(999);

        Assert.Equal(ServiceError.NotFound, result.Error);
    }

    // ---------- книга дня ----------

    [Fact]
    public async Task SetBookOfTheDay_ZeroId_ReturnsInvalid()
    {
        var result = await BuildService().SetBookOfTheDayAsync(DateTime.UtcNow, 0);

        Assert.Equal(ServiceError.Invalid, result.Error);
        Assert.Equal("Выберите книгу перед сохранением.", result.Message);
    }

    [Fact]
    public async Task SetBookOfTheDay_MissingBook_ReturnsInvalid()
    {
        var result = await BuildService().SetBookOfTheDayAsync(DateTime.UtcNow, 999);

        Assert.Equal(ServiceError.Invalid, result.Error);
    }

    [Fact]
    public async Task SetBookOfTheDay_HiddenBook_ReturnsInvalid()
    {
        _uow.AddBook(id: 1, hidden: true);

        var result = await BuildService().SetBookOfTheDayAsync(DateTime.UtcNow, 1);

        Assert.Equal(ServiceError.Invalid, result.Error);
    }

    [Fact]
    public async Task SetBookOfTheDay_NewDate_CreatesEntry()
    {
        _uow.AddBook(id: 1);

        var result = await BuildService().SetBookOfTheDayAsync(new DateTime(2026, 6, 18), 1);

        Assert.True(result.Ok);
        Assert.Equal(1, Assert.Single(_uow.BooksOfTheDay).BookId);
    }

    [Fact]
    public async Task SetBookOfTheDay_SameDateTwice_UpdatesExisting()
    {
        _uow.AddBook(id: 1);
        _uow.AddBook(id: 2);
        var service = BuildService();
        var day = new DateTime(2026, 6, 18);

        await service.SetBookOfTheDayAsync(day, 1);
        var result = await service.SetBookOfTheDayAsync(day, 2);

        Assert.True(result.Ok);
        Assert.Equal(2, Assert.Single(_uow.BooksOfTheDay).BookId);   // та же запись, новая книга — upsert
    }

    // ---------- квизы ----------

    [Fact]
    public async Task CreateQuiz_AddsQuestion()
    {
        var result = await BuildService().CreateQuizAsync(QuizForm(bookId: 1, correct: "Толстой"));

        Assert.True(result.Ok);
        Assert.Equal("Толстой", Assert.Single(_uow.QuizQuestions).CorrectAnswer);
    }

    [Fact]
    public async Task EditQuiz_MissingQuestion_ReturnsNotFound()
    {
        var result = await BuildService().EditQuizAsync(QuizForm(id: 999));

        Assert.Equal(ServiceError.NotFound, result.Error);
    }

    [Fact]
    public async Task EditQuiz_UpdatesFields()
    {
        _uow.QuizQuestions.Add(new QuizQuestion { Id = 1, BookId = 1, Quote = "старая", CorrectAnswer = "старый" });

        var result = await BuildService().EditQuizAsync(QuizForm(id: 1, correct: "новый"));

        Assert.True(result.Ok);
        var question = Assert.Single(_uow.QuizQuestions);
        Assert.Equal("новый", question.CorrectAnswer);
        Assert.Equal("Цитата", question.Quote);
    }

    [Fact]
    public async Task DeleteQuiz_RemovesQuestion()
    {
        _uow.QuizQuestions.Add(new QuizQuestion { Id = 1, BookId = 1 });

        var result = await BuildService().DeleteQuizAsync(1);

        Assert.True(result.Ok);
        Assert.Empty(_uow.QuizQuestions);
    }

    [Fact]
    public async Task DeleteQuiz_Missing_ReturnsNotFound()
    {
        var result = await BuildService().DeleteQuizAsync(999);

        Assert.Equal(ServiceError.NotFound, result.Error);
    }

    // ---------- пользователи (UserManager через Moq) ----------

    [Fact]
    public async Task ToggleUserBlock_FlipsAndPersists()
    {
        var user = new User { Id = "u1", UserName = "vasya", IsBlocked = false };
        var userManager = TestDoubles.MockUserManager("admin");
        userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var result = await BuildService(userManager).ToggleUserBlockAsync("u1");

        Assert.True(result.Ok);
        Assert.True(user.IsBlocked);
        userManager.Verify(m => m.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ToggleUserBlock_MissingUser_ReturnsNotFound()
    {
        var userManager = TestDoubles.MockUserManager("admin");
        userManager.Setup(m => m.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((User?)null);

        var result = await BuildService(userManager).ToggleUserBlockAsync("nope");

        Assert.Equal(ServiceError.NotFound, result.Error);
    }

    [Fact]
    public async Task ToggleUserAdmin_NotInRole_AddsRole()
    {
        var user = new User { Id = "u1", UserName = "vasya" };
        var userManager = TestDoubles.MockUserManager("admin");
        userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManager.Setup(m => m.IsInRoleAsync(user, It.IsAny<string>())).ReturnsAsync(false);
        userManager.Setup(m => m.AddToRoleAsync(user, It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        var result = await BuildService(userManager).ToggleUserAdminAsync("u1");

        Assert.True(result.Ok);
        userManager.Verify(m => m.AddToRoleAsync(user, It.IsAny<string>()), Times.Once);
        userManager.Verify(m => m.RemoveFromRoleAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }
}
