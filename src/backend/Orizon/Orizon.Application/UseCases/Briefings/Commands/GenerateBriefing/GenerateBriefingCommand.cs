using MediatR;

namespace Orizon.Application.UseCases.Briefings.Commands.GenerateBriefing;

public record GenerateBriefingCommand(string UserId) : IRequest<GenerateBriefingResult>;
public record GenerateBriefingResult(string JobId, string Message);