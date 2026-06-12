using BookSwap.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookSwap.Web.Helpers;

public static class ControllerExtensions
{
    public static IActionResult ToActionResult(this ServiceResult result) => result.Error switch
    {
        ServiceError.NotFound => new NotFoundResult(),
        ServiceError.Forbidden => new ForbidResult(),
        ServiceError.Invalid => new BadRequestObjectResult(result.Message),
        ServiceError.Conflict => new ConflictObjectResult(result.Message),
        _ => new OkResult()
    };
}
