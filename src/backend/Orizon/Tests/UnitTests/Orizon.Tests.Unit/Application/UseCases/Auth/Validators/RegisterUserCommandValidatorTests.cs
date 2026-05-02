using FluentAssertions;
using Orizon.Application.UseCases.Auth.Commands.RegisterUser;

namespace Orizon.Tests.Unit.Application.UseCases.Auth.Validators;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "Test@12345");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyDisplayName_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "", "aurel@orizonapp.io", "Test@12345");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "DisplayName");
    }

    [Fact]
    public void Validate_WhenInvalidEmail_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "email-invalido", "Test@12345");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_WhenPasswordTooShort_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "Ab1");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WhenPasswordHasNoUppercase_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "test@12345");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_WhenPasswordHasNoNumber_ShouldHaveError()
    {
        // Arrange
        var command = new RegisterUserCommand(
            "Aurel", "aurel@orizonapp.io", "TestSenha");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "Password");
    }
}