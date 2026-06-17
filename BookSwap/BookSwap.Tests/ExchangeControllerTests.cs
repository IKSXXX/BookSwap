using AutoMapper;
using BookSwap.Db.Entities;
using BookSwap.Tests.Testing;
using BookSwap.Web.Controllers;
using BookSwap.Web.Services;
using BookSwap.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace BookSwap.Tests;

public class ExchangeControllerTests
{
    private readonly TestUnitOfWork _uow = new();
    private readonly Mock<IExchangeService> _exchange = new();
    private readonly Mock<UserManager<User>> _userManager;

    public ExchangeControllerTests() => _userManager = TestDoubles.MockUserManager("user");

    private ExchangeController BuildController()
    {
        var controller = new ExchangeController(_uow, Mock.Of<IMapper>(), _userManager.Object, _exchange.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>()),
            Url = Mock.Of<IUrlHelper>()
        };
        return controller;
    }

    [Fact]
    public async Task Accept_WhenServiceSucceeds_RedirectsToDetails()
    {
        _exchange.Setup(s => s.AcceptAsync(10, "user")).ReturnsAsync(ServiceResult.Success());

        var result = await BuildController().Accept(10);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ExchangeController.Details), redirect.ActionName);
    }

    [Fact]
    public async Task Accept_WhenServiceForbids_ReturnsForbid()
    {
        _exchange.Setup(s => s.AcceptAsync(10, "user")).ReturnsAsync(ServiceResult.Fail(ServiceError.Forbidden));

        var result = await BuildController().Accept(10);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Cancel_WhenServiceForbids_ReturnsForbid()
    {
        _exchange.Setup(s => s.CancelAsync(10, "user")).ReturnsAsync(ServiceResult.Fail(ServiceError.Forbidden));

        var result = await BuildController().Cancel(10);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Complete_WhenServiceSucceeds_RedirectsToDetails()
    {
        _exchange.Setup(s => s.CompleteAsync(10, "user")).ReturnsAsync(ServiceResult.Success());

        var result = await BuildController().Complete(10);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ExchangeController.Details), redirect.ActionName);
    }

    [Fact]
    public async Task Create_Post_WhenServiceSucceeds_RedirectsToDetailsWithId()
    {
        _exchange.Setup(s => s.CreateAsync(5, "user", null, It.IsAny<Func<int, string?>>()))
            .ReturnsAsync(ServiceResult<int>.Success(42));

        var result = await BuildController().Create(5, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ExchangeController.Details), redirect.ActionName);
        Assert.Equal(42, redirect.RouteValues!["id"]);
    }

    [Fact]
    public async Task Create_Post_WhenServiceFails_ReturnsBadRequest()
    {
        _exchange.Setup(s => s.CreateAsync(5, "user", null, It.IsAny<Func<int, string?>>()))
            .ReturnsAsync(ServiceResult<int>.Fail(ServiceError.Invalid, "Нельзя обменяться с самим собой."));

        var result = await BuildController().Create(5, null);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task LeaveReview_Post_InvalidModelState_ReturnsViewWithModel()
    {
        var controller = BuildController();
        controller.ModelState.AddModelError("Rating", "Required");
        var model = new ReviewFormViewModel { ExchangeRequestId = 10, ToUserId = "receiver" };

        var result = await controller.LeaveReview(model);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(model, view.Model);
        _exchange.Verify(s => s.LeaveReviewAsync(It.IsAny<ReviewFormViewModel>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LeaveReview_Post_WhenServiceSucceeds_RedirectsAndSetsTempData()
    {
        _exchange.Setup(s => s.LeaveReviewAsync(It.IsAny<ReviewFormViewModel>(), "user"))
            .ReturnsAsync(ServiceResult.Success());
        var controller = BuildController();
        var model = new ReviewFormViewModel { ExchangeRequestId = 10, ToUserId = "receiver", Rating = 5 };

        var result = await controller.LeaveReview(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ExchangeController.Details), redirect.ActionName);
        Assert.Equal("Спасибо за отзыв!", controller.TempData["Success"]);
    }

    [Fact]
    public async Task Details_AsNonParticipant_ReturnsForbid()
    {
        _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("stranger");
        _uow.AddExchange(id: 10, "sender", "receiver", ExchangeStatus.Pending, requestedId: 1);

        var result = await BuildController().Details(10);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Create_Get_OwnBook_ReturnsBadRequest()
    {
        _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns("user-1");
        _uow.AddBook(id: 1, available: true);
        _uow.AddOwner(bookId: 1, userId: "user-1");

        var result = await BuildController().Create(bookId: 1);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
