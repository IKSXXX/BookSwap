using BookExchange.Db.Data;
using BookExchange.Db.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace BookExchange.Web.Hubs;

[Authorize]
public class DiscussionHub : Hub
{
    readonly BookExchangeDbContext _ctx;
    readonly UserManager<User> _um;

    public DiscussionHub(BookExchangeDbContext ctx, UserManager<User> um)
    {
        _ctx = ctx;
        _um = um;
    }

    public async Task JoinDiscussion(int discussionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"discussion-{discussionId}");
    }

    public async Task SendMessage(int discussionId, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000) return;

        var userId = _um.GetUserId(Context.User!);
        var user = await _um.FindByIdAsync(userId!);

        var msg = new DiscussionMessage
        {
            DiscussionId = discussionId,
            UserId = userId!,
            Text = text.Trim()
        };
        _ctx.DiscussionMessages.Add(msg);
        await _ctx.SaveChangesAsync();

        await Clients.Group($"discussion-{discussionId}").SendAsync("ReceiveMessage", new
        {
            userName = user?.UserName ?? "",
            text = msg.Text,
            createdAt = msg.CreatedAt
        });
    }
}
