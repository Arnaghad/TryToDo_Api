using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using TryToDo_Api.Classes;

public class DummyEmailSender : IEmailSender<AuthUser>
{
    public Task SendEmailAsync(AuthUser user, string subject, string htmlMessage)
    {
        // Do nothing or log the message
        return Task.CompletedTask;
    }

    public Task SendConfirmationLinkAsync(AuthUser user, string link, string callbackUrl)
    {
        // Do nothing or log the message
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(AuthUser user, string code, string callbackUrl)
    {
        // Do nothing or log the message
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(AuthUser user, string link, string callbackUrl)
    {
        // Do nothing or log the message
        return Task.CompletedTask;
    }
}