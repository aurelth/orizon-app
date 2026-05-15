using MediatR;
using Orizon.Application.Interfaces.Repositories;

namespace Orizon.Application.UseCases.Users.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandHandler : IRequestHandler<CompleteOnboardingCommand>
{
    private readonly IUserRepository _userRepository;

    public CompleteOnboardingCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(
        CompleteOnboardingCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(
            request.UserId, cancellationToken);

        if (user is null) return;

        user.HasCompletedOnboarding = true;

        await _userRepository.UpdateAsync(user, cancellationToken);
    }
}