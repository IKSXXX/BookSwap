using Microsoft.AspNetCore.Identity.UI.Services;

namespace BookExchange.Web.Helpers;

public class ConsoleEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"[EMAIL] To: {email} | Subject: {subject}");
        return Task.CompletedTask;
    }
}
