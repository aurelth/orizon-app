using FluentAssertions;
using Orizon.Application.UseCases.Auth.Commands.Logout;

namespace Orizon.Tests.Unit.Application.UseCases.Auth.Validators;

public class LogoutCommandValidatorTests
{
    private readonly LogoutCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new LogoutCommand(Guid.NewGuid().ToString());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var command = new LogoutCommand("");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == "UserId");
    }
}