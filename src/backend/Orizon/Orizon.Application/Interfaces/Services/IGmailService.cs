using Orizon.Application.DTOs.Email;

namespace Orizon.Application.Interfaces.Services;

public interface IGmailService
{
    Task<IEnumerable<EmailSummaryDto>> GetRecentEmailsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<EmailSummaryDto>> GetRecentEmailsWithTokenAsync(
        string accessToken,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}