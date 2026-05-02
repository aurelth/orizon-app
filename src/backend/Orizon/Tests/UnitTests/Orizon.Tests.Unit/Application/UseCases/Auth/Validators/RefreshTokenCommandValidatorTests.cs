using FluentAssertions;
using Orizon.Application.UseCases.Auth.Commands.RefreshToken;

namespace Orizon.Tests.Unit.Application.UseCases.Auth.Validators;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new RefreshTokenCommand("token-valido");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyToken_ShouldHaveError()
    {
        // Arrange
        var command = new RefreshTokenCommand("");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Token");
    }
}