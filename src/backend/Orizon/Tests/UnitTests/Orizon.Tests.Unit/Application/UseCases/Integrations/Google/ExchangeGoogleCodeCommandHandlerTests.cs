using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Repositories;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Integrations.Google.Command;
using Orizon.Domain.Entities;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Google;

public class ExchangeGoogleCodeCommandHandlerTests
{
    private readonly Mock<IGoogleOAuthService> _googleOAuthServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly ExchangeGoogleCodeCommandHandler _handler;

    public ExchangeGoogleCodeCommandHandlerTests()
    {
        _googleOAuthServiceMock = new Mock<IGoogleOAuthService>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new ExchangeGoogleCodeCommandHandler(
            _googleOAuthServiceMock.Object,
            _userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidCode_ShouldReturnTokens()
    {
        var userId = Guid.NewGuid();
        var command = new ExchangeGoogleCodeCommand(userId.ToString(), "auth-code-abc");
        var expectedTokens = new GoogleTokensDto(
            "access-token-xyz",
            "refresh-token-xyz",
            DateTime.UtcNow.AddHours(1));

        var mockUser = new AppUser { Id = userId, Email = "test@test.com" };

        _googleOAuthServiceMock
            .Setup(s => s.ExchangeCodeAsync(
                command.Code,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockUser);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be(expectedTokens.AccessToken);
        result.RefreshToken.Should().Be(expectedTokens.RefreshToken);
        result.ExpiresAt.Should().Be(expectedTokens.ExpiresAt);

        _userRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldStillReturnTokens()
    {
        var command = new ExchangeGoogleCodeCommand(Guid.NewGuid().ToString(), "auth-code-abc");
        var expectedTokens = new GoogleTokensDto(
            "access-token-xyz",
            "refresh-token-xyz",
            DateTime.UtcNow.AddHours(1));

        _googleOAuthServiceMock
            .Setup(s => s.ExchangeCodeAsync(
                command.Code,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AppUser?)null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be(expectedTokens.AccessToken);

        _userRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<AppUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_ShouldPropagateException()
    {
        var command = new ExchangeGoogleCodeCommand(Guid.NewGuid().ToString(), "invalid-code");

        _googleOAuthServiceMock
            .Setup(s => s.ExchangeCodeAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Código inválido"));

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("Código inválido");
    }
}