using System.Security.Claims;
using BookSwap.Db.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace BookSwap.Tests.Testing;

public static class TestDoubles
{
    public static Mock<UserManager<User>> MockUserManager(string currentUserId)
    {
        var store = new Mock<IUserStore<User>>();
        var userManager = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUserId);
        return userManager;
    }
}

public static class TestData
{
    public static Book AddBook(this TestUnitOfWork uow, int id, bool available = true, bool hidden = false)
    {
        var book = new Book { Id = id, Title = $"Книга {id}", Author = "Автор", IsAvailable = available, IsHidden = hidden };
        uow.Books.Add(book);
        return book;
    }

    public static BookOwner AddOwner(this TestUnitOfWork uow, int bookId, string userId, bool primary = true)
    {
        var book = uow.Books.FirstOrDefault(b => b.Id == bookId);
        var owner = new BookOwner { BookId = bookId, UserId = userId, IsPrimary = primary, Book = book };
        uow.BookOwners.Add(owner);
        book?.BookOwners.Add(owner);
        return owner;
    }

    public static ExchangeRequest AddExchange(this TestUnitOfWork uow, int id, string senderId, string receiverId,
        ExchangeStatus status, int requestedId, int? offeredId = null)
    {
        var exchange = new ExchangeRequest
        {
            Id = id,
            SenderId = senderId,
            ReceiverId = receiverId,
            Status = status,
            BookRequestedId = requestedId,
            BookOfferedId = offeredId
        };
        uow.Exchanges.Add(exchange);
        return exchange;
    }
}
