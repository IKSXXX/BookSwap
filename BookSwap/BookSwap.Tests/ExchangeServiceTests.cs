using BookSwap.Db.Entities;
using BookSwap.Tests.Testing;
using BookSwap.Web.Services;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace BookSwap.Tests;

public class ExchangeServiceTests
{
    private readonly TestUnitOfWork _uow = new();
    private readonly Mock<INotificationService> _notifications = new();

    private ExchangeService BuildService(Mock<UserManager<User>> userManager)
        => new(_uow, userManager.Object, _notifications.Object);

    private ExchangeService BuildService(string currentUserId = "user")
        => BuildService(TestDoubles.MockUserManager(currentUserId));

    [Fact]
    public async Task Accept_AsReceiver_SetsAcceptedAndMarksBooksUnavailable()
    {
        var requested = _uow.AddBook(id: 1, available: true);
        var offered = _uow.AddBook(id: 2, available: true);
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1, offeredId: 2);

        var result = await BuildService().AcceptAsync(10, "receiver");

        Assert.True(result.Ok);
        Assert.Equal(ExchangeStatus.Accepted, _uow.Exchanges.Single(e => e.Id == 10).Status);
        Assert.False(requested.IsAvailable);
        Assert.False(offered.IsAvailable);
    }

    [Fact]
    public async Task Accept_AsSender_ReturnsForbidden()
    {
        _uow.AddBook(id: 1, available: true);
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1);

        var result = await BuildService().AcceptAsync(10, "sender");

        Assert.Equal(ServiceError.Forbidden, result.Error);
        Assert.Equal(ExchangeStatus.Pending, _uow.Exchanges.Single(e => e.Id == 10).Status);
    }

    [Fact]
    public async Task Cancel_AsReceiver_ReturnsForbidden()
    {
        _uow.AddBook(id: 1, available: true);
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1);

        var result = await BuildService().CancelAsync(10, "receiver");

        Assert.Equal(ServiceError.Forbidden, result.Error);
        Assert.Equal(ExchangeStatus.Pending, _uow.Exchanges.Single(e => e.Id == 10).Status);
    }

    [Fact]
    public async Task Accept_WhenNotPending_ReturnsNotFound()
    {
        _uow.AddBook(id: 1, available: true);
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Accepted, requestedId: 1);

        var result = await BuildService().AcceptAsync(10, "receiver");

        Assert.Equal(ServiceError.NotFound, result.Error);
    }

    [Fact]
    public async Task Complete_TransfersBookOwnershipToOtherParty()
    {
        _uow.AddBook(id: 1, available: false);
        _uow.AddBook(id: 2, available: false);
        _uow.AddOwner(bookId: 1, userId: "receiver");
        _uow.AddOwner(bookId: 2, userId: "sender");
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Accepted, requestedId: 1, offeredId: 2);

        var result = await BuildService().CompleteAsync(10, "sender");

        Assert.True(result.Ok);
        Assert.Equal(ExchangeStatus.Completed, _uow.Exchanges.Single(e => e.Id == 10).Status);

        Assert.Contains(_uow.BookOwners, o => o.BookId == 1 && o.UserId == "sender" && o.IsPrimary);
        Assert.DoesNotContain(_uow.BookOwners, o => o.BookId == 1 && o.UserId == "receiver");
        Assert.Contains(_uow.BookOwners, o => o.BookId == 2 && o.UserId == "receiver" && o.IsPrimary);
        Assert.DoesNotContain(_uow.BookOwners, o => o.BookId == 2 && o.UserId == "sender");
    }

    [Fact]
    public async Task LeaveReview_RecalculatesTargetUserRating()
    {
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Completed, requestedId: 1);
        _uow.Reviews.Add(new Review { Id = 1, ToUserId = "receiver", FromUserId = "other", ExchangeRequestId = 99, Rating = 4 });

        var target = new User { Id = "receiver", UserName = "receiver" };
        var userManager = TestDoubles.MockUserManager("sender");
        userManager.Setup(m => m.FindByIdAsync("receiver")).ReturnsAsync(target);
        userManager.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);

        var model = new ReviewFormViewModel { ExchangeRequestId = 10, ToUserId = "receiver", Rating = 2 };
        var result = await BuildService(userManager).LeaveReviewAsync(model, "sender");

        Assert.True(result.Ok);
        Assert.Contains(_uow.Reviews, r => r.FromUserId == "sender" && r.ToUserId == "receiver");
        Assert.Equal(3.0, target.Rating);
    }

    [Fact]
    public async Task LeaveReview_WhenAlreadyReviewed_ReturnsInvalid()
    {
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Completed, requestedId: 1);
        _uow.Reviews.Add(new Review { Id = 1, ToUserId = "receiver", FromUserId = "sender", ExchangeRequestId = 10, Rating = 5 });

        var model = new ReviewFormViewModel { ExchangeRequestId = 10, ToUserId = "receiver", Rating = 2 };
        var result = await BuildService("sender").LeaveReviewAsync(model, "sender");

        Assert.Equal(ServiceError.Invalid, result.Error);
    }

    [Fact]
    public async Task Create_OwnBook_ReturnsInvalid()
    {
        _uow.AddBook(id: 1, available: true);
        _uow.AddOwner(bookId: 1, userId: "user-1");

        var result = await BuildService("user-1").CreateAsync(1, "user-1", null, _ => "/url");

        Assert.Equal(ServiceError.Invalid, result.Error);
    }

    [Fact]
    public async Task Create_NotifiesReceiverAndReturnsId()
    {
        _uow.AddBook(id: 1, available: true);
        _uow.AddOwner(bookId: 1, userId: "owner");

        var userManager = TestDoubles.MockUserManager("sender");
        userManager.Setup(m => m.FindByIdAsync("sender"))
            .ReturnsAsync(new User { Id = "sender", UserName = "sender" });

        var result = await BuildService(userManager).CreateAsync(1, "sender", null, id => $"/exchange/{id}");

        Assert.True(result.Ok);
        Assert.Contains(_uow.Exchanges, e => e.SenderId == "sender" && e.ReceiverId == "owner" && e.Status == ExchangeStatus.Pending);
        _notifications.Verify(n => n.NotifyAsync("owner", "exchange", It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }
}
