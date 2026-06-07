using MediatR;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;

namespace Orizon.Application.UseCases.Users.Commands.DeleteAccount;

public class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IIdentityService _identityService;
    private readonly IUserRepository _userRepository;

    public DeleteAccountCommandHandler(
        IIdentityService identityService,
        IUserRepository userRepository)
    {
        _identityService = identityService;
        _userRepository = userRepository;
    }

    public async Task Handle(
        DeleteAccountCommand request,
        CancellationToken cancellationToken)
    {
        // Valida a senha antes de excluir
        var passwordValid = await _identityService.CheckPasswordAsync(
            request.UserId.ToString(),
            request.Password,
            cancellationToken);

        if (!passwordValid)
            throw new InvalidOperationException("Senha incorreta.");

        // Remove todos os dados do usuário
        await _userRepository.DeleteAsync(request.UserId, cancellationToken);
    }
}