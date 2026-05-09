using MediatR;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Briefings.Commands.GenerateBriefing;

public class GenerateBriefingCommandHandler
    : IRequestHandler<GenerateBriefingCommand, GenerateBriefingResult>
{
    private readonly IJobScheduler _jobScheduler;

    public GenerateBriefingCommandHandler(IJobScheduler jobScheduler)
    {
        _jobScheduler = jobScheduler;
    }

    public async Task<GenerateBriefingResult> Handle(
        GenerateBriefingCommand request,
        CancellationToken cancellationToken)
    {
        var jobId = await _jobScheduler.EnqueueBriefingGenerationAsync(cancellationToken);

        return new GenerateBriefingResult(
            jobId,
            "Briefing sendo gerado. Aguarde alguns instantes.");
    }
}