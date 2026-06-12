namespace BookSwap.Web.Services;

public interface INotificationService
{
    Task NotifyAsync(string userId, string type, string text, string? relatedUrl = null);
}
