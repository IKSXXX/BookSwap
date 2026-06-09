using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace BookSwap.Web.Hubs;

[Authorize]
public class DiscussionHub : Hub
{
    readonly IUnitOfWork _uow;
    readonly UserManager<User> _um;

    public DiscussionHub(IUnitOfWork uow, UserManager<User> um)
    {
        _uow = uow;
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
        await _uow.DiscussionMessages.AddAsync(msg);
        await _uow.SaveChangesAsync();

        await Clients.Group($"discussion-{discussionId}").SendAsync("ReceiveMessage", new
        {
            userName = user?.UserName ?? "",
            text = msg.Text,
            createdAt = msg.CreatedAt
        });
    }
}
