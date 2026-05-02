using FluentAssertions;
using Orizon.Application.UseCases.Auth.Commands.Login;

namespace Orizon.Tests.Unit.Application.UseCases.Auth.Validators;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new LoginCommand("aurel@orizonapp.io", "Test@12345");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyEmail_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("", "Test@12345");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WhenInvalidEmail_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("email-invalido", "Test@12345");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WhenEmptyPassword_ShouldHaveError()
    {
        // Arrange
        var command = new LoginCommand("aurel@orizonapp.io", "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Password");
    }
}