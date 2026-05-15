using MediatR;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Briefings.Commands.GenerateBriefing;

public class GenerateBriefingCommandHandler
    : IRequestHandler<GenerateBriefingCommand, GenerateBriefingResult>
{
    private readonly IJobScheduler _jobScheduler;
    private readonly IUserRepository _userRepository;

    public GenerateBriefingCommandHandler(
        IJobScheduler jobScheduler,
        IUserRepository userRepository)
    {
        _jobScheduler = jobScheduler;
        _userRepository = userRepository;
    }

    public async Task<GenerateBriefingResult> Handle(
        GenerateBriefingCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
            throw new InvalidOperationException("Usuário inválido.");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        var hasGoogleConnected = !string.IsNullOrEmpty(user.GoogleAccessToken);
        var hasTrelloConnected = user.TrelloEnabled;

        if (!hasGoogleConnected && !hasTrelloConnected)
            throw new InvalidOperationException(
                "É necessário conectar ao menos uma integração (Google ou Trello) antes de gerar o briefing.");

        var jobId = await _jobScheduler.EnqueueBriefingGenerationAsync(cancellationToken);

        return new GenerateBriefingResult(
            jobId,
            "Briefing sendo gerado. Aguarde alguns instantes.");
    }
}