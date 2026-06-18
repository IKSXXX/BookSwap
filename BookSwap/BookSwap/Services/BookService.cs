using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.ViewModels;

namespace BookSwap.Web.Services;

public class BookService : IBookService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public BookService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ServiceResult<int>> CreateAsync(BookFormViewModel model, string userId, string? coverPath)
    {
        var book = _mapper.Map<Book>(model);
        if (coverPath != null) book.CoverImagePath = coverPath;

        await _uow.Books.AddAsync(book);
        await _uow.SaveChangesAsync();

        await _uow.BookOwners.AddAsync(new BookOwner { BookId = book.Id, UserId = userId, IsPrimary = true });
        await _uow.SaveChangesAsync();

        return ServiceResult<int>.Success(book.Id);
    }

    public async Task<ServiceResult<BookFormViewModel>> GetForEditAsync(int bookId, string userId)
    {
        var book = await _uow.Books.GetByIdAsync(bookId);
        if (book == null) return ServiceResult<BookFormViewModel>.Fail(ServiceError.Forbidden);
        if (!await IsOwnerAsync(bookId, userId)) return ServiceResult<BookFormViewModel>.Fail(ServiceError.Forbidden);

        return ServiceResult<BookFormViewModel>.Success(_mapper.Map<BookFormViewModel>(book));
    }

    public async Task<ServiceResult> EditAsync(BookFormViewModel model, string userId, string? coverPath)
    {
        if (model.Id == null) return ServiceResult.Fail(ServiceError.Invalid);

        var book = await _uow.Books.GetByIdAsync(model.Id.Value);
        if (book == null) return ServiceResult.Fail(ServiceError.Forbidden);
        if (!await IsOwnerAsync(model.Id.Value, userId)) return ServiceResult.Fail(ServiceError.Forbidden);

        book.Title = model.Title;
        book.Author = model.Author;
        book.ISBN = model.ISBN;
        book.Description = model.Description;
        book.Genre = model.Genre;
        book.Condition = model.Condition;
        book.Year = model.Year;
        book.Language = model.Language;
        if (coverPath != null) book.CoverImagePath = coverPath;

        _uow.Books.Update(book);
        await _uow.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(int bookId, string userId)
    {
        var book = await _uow.Books.GetByIdAsync(bookId);
        if (book == null) return ServiceResult.Fail(ServiceError.Forbidden);
        if (!await IsOwnerAsync(bookId, userId)) return ServiceResult.Fail(ServiceError.Forbidden);

        _uow.Books.Remove(book);
        await _uow.SaveChangesAsync();
        return ServiceResult.Success();
    }

    Task<bool> IsOwnerAsync(int bookId, string userId)
        => _uow.BookOwners.AnyAsync(bo => bo.BookId == bookId && bo.UserId == userId);
}
