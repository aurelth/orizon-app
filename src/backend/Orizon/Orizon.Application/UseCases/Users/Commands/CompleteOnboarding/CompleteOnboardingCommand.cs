using MediatR;

namespace Orizon.Application.UseCases.Users.Commands.CompleteOnboarding;

public record CompleteOnboardingCommand(Guid UserId) : IRequest;