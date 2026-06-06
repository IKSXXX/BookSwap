using System.Security.Claims;
using AutoMapper;
using BookExchange.Db.Entities;
using BookExchange.Web.Controllers;
using BookExchange.Web.Hubs;
using BookExchange.Web.Mocks;
using BookExchange.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace BookExchange.Tests;

/// <summary>
/// Юнит-тесты сценария обмена. Работают на готовом MockUnitOfWork/MockDataStore
/// (тот же режим, что UseMockData), без реальной БД и сети.
/// MockDataStore статичен, поэтому каждый тест очищает его в конструкторе.
/// </summary>
public class ExchangeControllerTests
{
    public ExchangeControllerTests() => ResetStore();

    [Fact]
    public async Task Accept_AsReceiver_SetsAcceptedAndMarksBooksUnavailable()
    {
        var requested = AddBook(id: 1, available: true);
        var offered = AddBook(id: 2, available: true);
        AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1, offeredId: 2);

        var controller = BuildController(currentUserId: "receiver");
        var result = await controller.Accept(10);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ExchangeStatus.Accepted, GetExchange(10).Status);
        Assert.False(requested.IsAvailable);
        Assert.False(offered.IsAvailable);
    }

    [Fact]
    public async Task Accept_AsSender_ReturnsForbid()
    {
        AddBook(id: 1, available: true);
        AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1);

        var controller = BuildController(currentUserId: "sender");
        var result = await controller.Accept(10);

        Assert.IsType<ForbidResult>(result);
        Assert.Equal(ExchangeStatus.Pending, GetExchange(10).Status);
    }


    [Fact]
    public async Task Cancel_AsReceiver_ReturnsForbid()
    {
        AddBook(id: 1, available: true);
        AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1);

        var controller = BuildController(currentUserId: "receiver");
        var result = await controller.Cancel(10);

        Assert.IsType<ForbidResult>(result);
        Assert.Equal(ExchangeStatus.Pending, GetExchange(10).Status);
    }

    [Fact]
    public async Task Complete_TransfersBookOwnershipToOtherParty()
    {
        AddBook(id: 1, available: false);
        AddBook(id: 2, available: false);
        AddOwner(bookId: 1, userId: "receiver");
        AddOwner(bookId: 2, userId: "sender");
        AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Accepted, requestedId: 1, offeredId: 2);

        var controller = BuildController(currentUserId: "sender");
        var result = await controller.Complete(10);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ExchangeStatus.Completed, GetExchange(10).Status);

        Assert.Contains(MockDataStore.BookOwners, o => o.BookId == 1 && o.UserId == "sender" && o.IsPrimary);
        Assert.DoesNotContain(MockDataStore.BookOwners, o => o.BookId == 1 && o.UserId == "receiver");
        Assert.Contains(MockDataStore.BookOwners, o => o.BookId == 2 && o.UserId == "receiver" && o.IsPrimary);
        Assert.DoesNotContain(MockDataStore.BookOwners, o => o.BookId == 2 && o.UserId == "sender");
    }

    [Fact]
    public async Task Details_AsNonParticipant_ReturnsForbid()
    {
        AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1);

        var controller = BuildController(currentUserId: "stranger");
        var result = await controller.Details(10);

        Assert.IsType<ForbidResult>(result);
    }


    [Fact]
    public async Task LeaveReview_RecalculatesTargetUserRating()
    {
        AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Completed, requestedId: 1);
        MockDataStore.Reviews.Add(new Review { Id = 1, ToUserId = "receiver", FromUserId = "other", ExchangeRequestId = 99, Rating = 4 });

        var target = new User { Id = "receiver", UserName = "receiver" };
        var um = MockUserManager("sender");
        um.Setup(m => m.FindByIdAsync("receiver")).ReturnsAsync(target);
        um.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        var controller = BuildController(um);
        var model = new ReviewFormViewModel { ExchangeRequestId = 10, ToUserId = "receiver", Rating = 2 };

        var result = await controller.LeaveReview(model);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains(MockDataStore.Reviews, r => r.FromUserId == "sender" && r.ToUserId == "receiver");
        Assert.Equal(3.0, target.Rating);
    }


    [Fact]
    public async Task Create_OwnBook_ReturnsBadRequest()
    {
        AddBook(id: 1, available: true);
        AddOwner(bookId: 1, userId: "user-1");

        var controller = BuildController(currentUserId: "user-1");
        var result = await controller.Create(bookId: 1); // обмен с самим собой запрещён

        Assert.IsType<BadRequestObjectResult>(result);
    }

    static void ResetStore()
    {
        MockDataStore.Books.Clear();
        MockDataStore.BookOwners.Clear();
        MockDataStore.Exchanges.Clear();
        MockDataStore.Messages.Clear();
        MockDataStore.Reviews.Clear();
        MockDataStore.Favorites.Clear();
        MockDataStore.Discussions.Clear();
        MockDataStore.DiscussionMessages.Clear();
        MockDataStore.QuizQuestions.Clear();
        MockDataStore.BooksOfTheDay.Clear();
        MockDataStore.Notifications.Clear();
    }

    static Book AddBook(int id, bool available)
    {
        var book = new Book { Id = id, Title = $"Книга {id}", Author = "Автор", IsAvailable = available };
        MockDataStore.Books.Add(book);
        return book;
    }

    static void AddOwner(int bookId, string userId)
        => MockDataStore.BookOwners.Add(new BookOwner { BookId = bookId, UserId = userId, IsPrimary = true });

    static void AddExchange(int id, string senderId, string receiverId, ExchangeStatus status, int requestedId, int? offeredId = null)
        => MockDataStore.Exchanges.Add(new ExchangeRequest
        {
            Id = id,
            SenderId = senderId,
            ReceiverId = receiverId,
            Status = status,
            BookRequestedId = requestedId,
            BookOfferedId = offeredId
        });

    static ExchangeRequest GetExchange(int id) => MockDataStore.Exchanges.First(e => e.Id == id);

    static Mock<UserManager<User>> MockUserManager(string currentUserId)
    {
        var store = new Mock<IUserStore<User>>();
        var um = new Mock<UserManager<User>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        um.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(currentUserId);
        return um;
    }

    static ExchangeController BuildController(string currentUserId)
        => BuildController(MockUserManager(currentUserId));

    static ExchangeController BuildController(Mock<UserManager<User>> um)
    {
        var controller = new ExchangeController(
            new MockUnitOfWork(),
            Mock.Of<IMapper>(),
            um.Object,
            Mock.Of<IHubContext<ChatHub>>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>())
        };
        return controller;
    }
}
