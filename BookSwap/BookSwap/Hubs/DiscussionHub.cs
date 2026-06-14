using BookSwap.Db.Entities;
using BookSwap.Db.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace BookSwap.Web.Hubs;

[Authorize]
public class DiscussionHub : Hub
{
    private readonly IUnitOfWork _uow;
    private readonly UserManager<User> _userManager;

    public DiscussionHub(IUnitOfWork uow, UserManager<User> userManager)
    {
        _uow = uow;
        _userManager = userManager;
    }

    public async Task JoinDiscussion(int discussionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"discussion-{discussionId}");
    }

    public async Task SendMessage(int discussionId, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 2000) return;

        var userId = _userManager.GetUserId(Context.User!);
        var user = await _userManager.FindByIdAsync(userId!);

        var message = new DiscussionMessage
        {
            DiscussionId = discussionId,
            UserId = userId!,
            Text = text.Trim()
        };
        await _uow.DiscussionMessages.AddAsync(message);
        await _uow.SaveChangesAsync();

        await Clients.Group($"discussion-{discussionId}").SendAsync("ReceiveMessage", new
        {
            userName = user?.UserName ?? "",
            text = message.Text,
            createdAt = message.CreatedAt
        });
    }
}
