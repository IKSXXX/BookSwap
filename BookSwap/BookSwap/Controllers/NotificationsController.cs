using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookSwap.Web.Controllers;

[Authorize]
[Route("notifications")]
public class NotificationsController : Controller
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<User> _userManager;

    public NotificationsController(IUnitOfWork uow, UserManager<User> userManager)
    {
        _uow = uow;
        _userManager = userManager;
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
    {
        var userId = _userManager.GetUserId(User)!;
        var list = await _uow.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new
            {
                n.Id,
                n.Text,
                n.Type,
                n.RelatedUrl,
                n.CreatedAt
            })
            .ToListAsync();
        return Json(list);
    }

    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var notification = await _uow.Notifications.GetByIdAsync(id);
        if (notification == null || notification.UserId != userId) return NotFound();
        notification.IsRead = true;
        _uow.Notifications.Update(notification);
        await _uow.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User)!;
        var unread = await _uow.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var notification in unread) notification.IsRead = true;
        await _uow.SaveChangesAsync();
        return Ok();
    }
}
