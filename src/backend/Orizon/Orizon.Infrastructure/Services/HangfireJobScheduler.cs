using Microsoft.Extensions.Configuration;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Infrastructure.Services;

public class HangfireJobScheduler : IJobScheduler
{
    private readonly HttpClient _httpClient;
    private readonly string _workerUrl;

    public HangfireJobScheduler(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _workerUrl = configuration["Worker:InternalUrl"]
            ?? "http://localhost:5011";
    }

    public async Task<string> EnqueueBriefingGenerationAsync(CancellationToken ct = default)
    {
        await _httpClient.PostAsync(
            $"{_workerUrl}/internal/briefing/trigger", null, ct);
        return "triggered";
    }
}