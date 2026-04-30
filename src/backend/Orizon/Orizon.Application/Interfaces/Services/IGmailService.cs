using Orizon.Application.DTOs.Email;

namespace Orizon.Application.Interfaces.Services;

public interface IGmailService
{    
    Task<IEnumerable<EmailSummaryDto>> GetRecentEmailsAsync(
        string userId,
        CancellationToken cancellationToken = default);
}