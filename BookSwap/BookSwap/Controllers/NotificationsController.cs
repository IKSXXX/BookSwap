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
    readonly IUnitOfWork _uow;
    readonly UserManager<User> _um;

    public NotificationsController(IUnitOfWork uow, UserManager<User> um)
    {
        _uow = uow;
        _um = um;
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnread()
    {
        var userId = _um.GetUserId(User)!;
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
        var userId = _um.GetUserId(User)!;
        var n = await _uow.Notifications.GetByIdAsync(id);
        if (n == null || n.UserId != userId) return NotFound();
        n.IsRead = true;
        _uow.Notifications.Update(n);
        await _uow.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _um.GetUserId(User)!;
        var unread = await _uow.Notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await _uow.SaveChangesAsync();
        return Ok();
    }
}
