using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Tests.Testing;
using BookSwap.Web.Services;
using BookSwap.Web.ViewModels;
using Moq;
using Xunit;

namespace BookSwap.Tests;

public class BookServiceTests
{
    private readonly TestUnitOfWork _uow = new();

    private BookService BuildService(IMapper? mapper = null) => new(_uow, mapper ?? Mock.Of<IMapper>());

    private static IMapper FormMapper()
    {
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<Book>(It.IsAny<BookFormViewModel>()))
            .Returns((BookFormViewModel m) => new Book
            {
                Title = m.Title,
                Author = m.Author,
                ISBN = m.ISBN,
                Description = m.Description,
                Genre = m.Genre,
                Condition = m.Condition,
                Year = m.Year,
                Language = m.Language
            });
        return mapper.Object;
    }

    [Fact]
    public async Task Create_AddsBookWithCoverAndPrimaryOwner()
    {
        var model = new BookFormViewModel { Title = "T", Author = "A", Genre = "Фантастика" };

        var result = await BuildService(FormMapper()).CreateAsync(model, "user-1", "/images/books/cover.jpg");

        Assert.True(result.Ok);
        var book = Assert.Single(_uow.Books);
        Assert.Equal("T", book.Title);
        Assert.Equal("/images/books/cover.jpg", book.CoverImagePath);
        Assert.Equal(book.Id, result.Value);
        Assert.Contains(_uow.BookOwners, o => o.BookId == book.Id && o.UserId == "user-1" && o.IsPrimary);
    }

    [Fact]
    public async Task Create_WithoutCover_LeavesCoverNull()
    {
        var model = new BookFormViewModel { Title = "T", Author = "A" };

        var result = await BuildService(FormMapper()).CreateAsync(model, "user-1", null);

        Assert.True(result.Ok);
        Assert.Null(Assert.Single(_uow.Books).CoverImagePath);
    }

    [Fact]
    public async Task GetForEdit_AsOwner_ReturnsForm()
    {
        _uow.AddBook(id: 1);
        _uow.AddOwner(bookId: 1, userId: "owner");
        var mapper = new Mock<IMapper>();
        mapper.Setup(m => m.Map<BookFormViewModel>(It.IsAny<Book>()))
            .Returns((Book b) => new BookFormViewModel { Id = b.Id, Title = b.Title });

        var result = await BuildService(mapper.Object).GetForEditAsync(1, "owner");

        Assert.True(result.Ok);
        Assert.Equal(1, result.Value!.Id);
    }

    [Fact]
    public async Task GetForEdit_AsNonOwner_ReturnsForbidden()
    {
        _uow.AddBook(id: 1);
        _uow.AddOwner(bookId: 1, userId: "owner");

        var result = await BuildService().GetForEditAsync(1, "stranger");

        Assert.Equal(ServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task GetForEdit_MissingBook_ReturnsForbidden()
    {
        var result = await BuildService().GetForEditAsync(999, "owner");

        Assert.Equal(ServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task Edit_AsOwner_UpdatesFields()
    {
        var book = _uow.AddBook(id: 1);
        _uow.AddOwner(bookId: 1, userId: "owner");
        var model = new BookFormViewModel { Id = 1, Title = "Новое", Author = "Новый автор", Genre = "Драма" };

        var result = await BuildService().EditAsync(model, "owner", "/images/books/new.jpg");

        Assert.True(result.Ok);
        Assert.Equal("Новое", book.Title);
        Assert.Equal("Новый автор", book.Author);
        Assert.Equal("Драма", book.Genre);
        Assert.Equal("/images/books/new.jpg", book.CoverImagePath);
    }

    [Fact]
    public async Task Edit_AsNonOwner_ReturnsForbiddenAndDoesNotChange()
    {
        var book = _uow.AddBook(id: 1);
        _uow.AddOwner(bookId: 1, userId: "owner");
        var model = new BookFormViewModel { Id = 1, Title = "Взлом", Author = "X" };

        var result = await BuildService().EditAsync(model, "stranger", null);

        Assert.Equal(ServiceError.Forbidden, result.Error);
        Assert.Equal("Книга 1", book.Title);
    }

    [Fact]
    public async Task Edit_MissingBook_ReturnsForbidden()
    {
        var model = new BookFormViewModel { Id = 999, Title = "T", Author = "A" };

        var result = await BuildService().EditAsync(model, "owner", null);

        Assert.Equal(ServiceError.Forbidden, result.Error);
    }

    [Fact]
    public async Task Edit_NullId_ReturnsInvalid()
    {
        var model = new BookFormViewModel { Id = null, Title = "T", Author = "A" };

        var result = await BuildService().EditAsync(model, "owner", null);

        Assert.Equal(ServiceError.Invalid, result.Error);
    }

    [Fact]
    public async Task Delete_AsOwner_RemovesBook()
    {
        _uow.AddBook(id: 1);
        _uow.AddOwner(bookId: 1, userId: "owner");

        var result = await BuildService().DeleteAsync(1, "owner");

        Assert.True(result.Ok);
        Assert.Empty(_uow.Books);
    }

    [Fact]
    public async Task Delete_AsNonOwner_ReturnsForbiddenAndKeepsBook()
    {
        _uow.AddBook(id: 1);
        _uow.AddOwner(bookId: 1, userId: "owner");

        var result = await BuildService().DeleteAsync(1, "stranger");

        Assert.Equal(ServiceError.Forbidden, result.Error);
        Assert.Single(_uow.Books);
    }

    [Fact]
    public async Task Delete_MissingBook_ReturnsForbidden()
    {
        var result = await BuildService().DeleteAsync(999, "owner");

        Assert.Equal(ServiceError.Forbidden, result.Error);
    }
}
