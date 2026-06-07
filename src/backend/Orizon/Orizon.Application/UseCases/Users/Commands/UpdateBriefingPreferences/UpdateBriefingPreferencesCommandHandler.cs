using MediatR;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Users.Commands.UpdateBriefingPreferences;

public class UpdateBriefingPreferencesCommandHandler
    : IRequestHandler<UpdateBriefingPreferencesCommand>
{
    private readonly IUserRepository _userRepository;

    public UpdateBriefingPreferencesCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(
        UpdateBriefingPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("Usuário não encontrado.");

        // Valida que a hora está no intervalo permitido (0-23)
        if (request.BriefingHour < 0 || request.BriefingHour > 23)
            throw new InvalidOperationException("Hora do briefing deve ser entre 0 e 23.");

        user.BriefingHour = request.BriefingHour;
        user.EmailSectionEnabled = request.EmailSectionEnabled;
        user.CalendarSectionEnabled = request.CalendarSectionEnabled;
        user.TrelloSectionEnabled = request.TrelloSectionEnabled;
        user.TasksSectionEnabled = request.TasksSectionEnabled;
        user.WeatherSectionEnabled = request.WeatherSectionEnabled;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}