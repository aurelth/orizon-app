using Orizon.Application.DTOs.Briefing;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services.Email;

public class NullEmailNotificationService : IEmailNotificationService
{
    public Task SendBriefingEmailAsync(
        string toEmail,
        string userName,
        BriefingResultDto briefing,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(
        string toEmail,
        string userName,
        string resetToken,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}