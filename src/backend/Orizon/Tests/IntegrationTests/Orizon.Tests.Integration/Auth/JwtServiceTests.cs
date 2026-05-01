using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Orizon.Domain.Entities;
using Orizon.Domain.Enums;
using Orizon.Infrastructure.Services;

namespace Orizon.Tests.Integration.Auth;

public class JwtServiceTests
{
    private readonly JwtService _jwtService;
    private readonly AppUser _testUser;

    public JwtServiceTests()
    {
        // ConfigurationBuilder real em memória => Não precisa de Mock        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Jwt:Secret", "TestSecretKey2026SuperSeguroParaJwtOrizon!!" },
                { "Jwt:ExpiryHours", "1" },
                { "Jwt:Issuer", "orizonapp.io" },
                { "Jwt:Audience", "orizonapp.io" }
            })
            .Build();
        
        _jwtService = new JwtService(configuration);

        _testUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "aurel@orizonapp.io",
            DisplayName = "Aurel",
            ThemePreference = ThemePreference.Dark
        };
    }

    [Fact]
    public void GenerateToken_WhenCalledWithValidUser_ShouldReturnNonEmptyString()
    {
        // Act
        var token = _jwtService.GenerateToken(_testUser);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_WhenCalledWithValidUser_ShouldReturnValidJwtFormat()
    {
        // Act
        var token = _jwtService.GenerateToken(_testUser);

        // Assert
        var parts = token.Split('.');
        parts.Should().HaveCount(3);
    }

    [Fact]
    public void GenerateToken_WhenCalledTwiceWithSameUser_ShouldReturnDifferentTokens()
    {
        // Act
        var token1 = _jwtService.GenerateToken(_testUser);
        var token2 = _jwtService.GenerateToken(_testUser);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateToken_WhenTokenIsValid_ShouldReturnTrue()
    {
        // Arrange
        var token = _jwtService.GenerateToken(_testUser);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WhenTokenIsInvalid_ShouldReturnFalse()
    {
        // Act
        var isValid = _jwtService.ValidateToken("token.invalido.aqui");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WhenTokenIsEmpty_ShouldReturnFalse()
    {
        // Act
        var isValid = _jwtService.ValidateToken(string.Empty);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WhenTokenIsTamperedWith_ShouldReturnFalse()
    {
        // Arrange
        var token = _jwtService.GenerateToken(_testUser);
        var parts = token.Split('.');
        var tamperedToken = $"{parts[0]}.PAYLOAD_ADULTERADO.{parts[2]}";

        // Act
        var isValid = _jwtService.ValidateToken(tamperedToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GetUserIdFromToken_WhenTokenIsValid_ShouldReturnCorrectUserId()
    {
        // Arrange
        var token = _jwtService.GenerateToken(_testUser);

        // Act
        var userId = _jwtService.GetUserIdFromToken(token);

        // Assert
        userId.Should().NotBeNull();
        userId.Should().Be(_testUser.Id);
    }

    [Fact]
    public void GetUserIdFromToken_WhenTokenIsInvalid_ShouldReturnNull()
    {
        // Act
        var userId = _jwtService.GetUserIdFromToken("token.invalido.aqui");

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserIdFromToken_WhenTokenIsEmpty_ShouldReturnNull()
    {
        // Act
        var userId = _jwtService.GetUserIdFromToken(string.Empty);

        // Assert
        userId.Should().BeNull();
    }
}