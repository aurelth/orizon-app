using FluentValidation;

namespace Orizon.Application.UseCases.Auth.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.");

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter no mínimo 8 caracteres.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$")
            .WithMessage("Senha deve conter maiúscula, minúscula e número.");
    }
}