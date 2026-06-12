using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using BookSwap.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BookSwap.Web.Services;

public class NotificationService : INotificationService
{
    readonly IUnitOfWork _uow;
    readonly IHubContext<ChatHub> _hub;

    public NotificationService(IUnitOfWork uow, IHubContext<ChatHub> hub)
    {
        _uow = uow;
        _hub = hub;
    }

    public async Task NotifyAsync(string userId, string type, string text, string? relatedUrl = null)
    {
        var notif = new Notification
        {
            UserId = userId,
            Type = type,
            Text = text,
            RelatedUrl = relatedUrl
        };
        await _uow.Notifications.AddAsync(notif);
        await _uow.SaveChangesAsync();

        try
        {
            await _hub.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                id = notif.Id,
                text = notif.Text,
                type = notif.Type,
                url = notif.RelatedUrl
            });
        }
        catch { }
    }
}
