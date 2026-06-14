namespace Orizon.Application.Interfaces.Services;

public interface IJobScheduler
{
    Task<string> EnqueueBriefingGenerationAsync(
        string userId,
        CancellationToken ct = default);
}