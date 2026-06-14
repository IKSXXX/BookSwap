using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace BookSwap.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<User> _userManager;

    public ChatHub(IUnitOfWork uow, UserManager<User> userManager)
    {
        _uow = uow;
        _userManager = userManager;
    }

    public async Task JoinGroup(int exchangeId)
    {
        var userId = _userManager.GetUserId(Context.User!);
        var exchange = await _uow.Exchanges.GetByIdAsync(exchangeId);
        if (exchange == null) return;
        if (exchange.SenderId != userId && exchange.ReceiverId != userId) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"exchange-{exchangeId}");
    }

    public async Task SendMessage(int exchangeId, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000) return;

        var userId = _userManager.GetUserId(Context.User!);
        var exchange = await _uow.Exchanges.GetByIdAsync(exchangeId);
        if (exchange == null) return;
        if (exchange.SenderId != userId && exchange.ReceiverId != userId) return;

        var user = await _userManager.FindByIdAsync(userId!);

        var message = new Message
        {
            ExchangeRequestId = exchangeId,
            SenderId = userId!,
            Text = text.Trim(),
            SentAt = DateTime.UtcNow
        };
        await _uow.Messages.AddAsync(message);
        await _uow.SaveChangesAsync();

        await Clients.Group($"exchange-{exchangeId}").SendAsync("ReceiveMessage", new
        {
            senderId = userId,
            senderName = user?.UserName ?? "",
            text = message.Text,
            sentAt = message.SentAt
        });
    }
}
