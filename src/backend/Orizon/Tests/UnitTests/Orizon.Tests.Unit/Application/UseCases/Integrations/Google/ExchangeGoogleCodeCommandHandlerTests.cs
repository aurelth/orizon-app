using FluentAssertions;
using Moq;
using Orizon.Application.Interfaces.Services;
using Orizon.Application.UseCases.Integrations.Google.Command;

namespace Orizon.Tests.Unit.Application.UseCases.Integrations.Google;

public class ExchangeGoogleCodeCommandHandlerTests
{
    private readonly Mock<IGoogleOAuthService> _googleOAuthServiceMock;
    private readonly ExchangeGoogleCodeCommandHandler _handler;

    public ExchangeGoogleCodeCommandHandlerTests()
    {
        _googleOAuthServiceMock = new Mock<IGoogleOAuthService>();
        _handler = new ExchangeGoogleCodeCommandHandler(_googleOAuthServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenValidCode_ShouldReturnTokens()
    {
        var command = new ExchangeGoogleCodeCommand("user-123", "auth-code-abc");
        var expectedTokens = new GoogleTokensDto(
            "access-token-xyz",
            "refresh-token-xyz",
            DateTime.UtcNow.AddHours(1));

        _googleOAuthServiceMock
            .Setup(s => s.ExchangeCodeAsync(
                command.Code,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.AccessToken.Should().Be(expectedTokens.AccessToken);
        result.RefreshToken.Should().Be(expectedTokens.RefreshToken);
        result.ExpiresAt.Should().Be(expectedTokens.ExpiresAt);
    }

    [Fact]
    public async Task Handle_WhenServiceThrows_ShouldPropagateException()
    {
        var command = new ExchangeGoogleCodeCommand("user-123", "invalid-code");

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