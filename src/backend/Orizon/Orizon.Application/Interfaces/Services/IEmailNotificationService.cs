using Orizon.Application.DTOs.Briefing;

namespace Orizon.Application.Interfaces.Services;

public interface IEmailNotificationService
{    
    Task SendBriefingEmailAsync(
        string toEmail,
        string userName,
        BriefingResultDto briefing,
        CancellationToken cancellationToken = default);
}