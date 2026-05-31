using BookExchange.Db.Data;
using BookExchange.Db.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BookExchange.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    readonly BookExchangeDbContext _ctx;
    readonly UserManager<User> _um;

    public ChatHub(BookExchangeDbContext ctx, UserManager<User> um)
    {
        _ctx = ctx;
        _um = um;
    }

    public async Task JoinGroup(int exchangeId)
    {
        var userId = _um.GetUserId(Context.User!);
        var exchange = await _ctx.ExchangeRequests.FindAsync(exchangeId);
        if (exchange == null) return;
        if (exchange.SenderId != userId && exchange.ReceiverId != userId) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"exchange-{exchangeId}");
    }

    public async Task SendMessage(int exchangeId, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000) return;

        var userId = _um.GetUserId(Context.User!);
        var exchange = await _ctx.ExchangeRequests.FindAsync(exchangeId);
        if (exchange == null) return;
        if (exchange.SenderId != userId && exchange.ReceiverId != userId) return;

        var user = await _um.FindByIdAsync(userId!);

        var msg = new Message
        {
            ExchangeRequestId = exchangeId,
            SenderId = userId!,
            Text = text.Trim(),
            SentAt = DateTime.UtcNow
        };
        _ctx.Messages.Add(msg);
        await _ctx.SaveChangesAsync();

        await Clients.Group($"exchange-{exchangeId}").SendAsync("ReceiveMessage", new
        {
            senderId = userId,
            senderName = user?.UserName ?? "",
            text = msg.Text,
            sentAt = msg.SentAt
        });
    }
}
