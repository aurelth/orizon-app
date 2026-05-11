namespace Orizon.Application.Interfaces.Services;

public interface IJobScheduler
{
    Task<string> EnqueueBriefingGenerationAsync(CancellationToken ct = default);
}