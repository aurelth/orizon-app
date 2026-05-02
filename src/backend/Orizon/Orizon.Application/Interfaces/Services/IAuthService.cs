using Orizon.Application.DTOs.Auth;

namespace Orizon.Application.Interfaces.Services;

public interface IAuthService
{    
    Task<AuthResponseDto> RegisterAsync(
        RegisterRequestDto request,
        CancellationToken ct = default);
 
    Task<AuthResponseDto> LoginAsync(
        LoginRequestDto request,
        CancellationToken ct = default);
    
    Task<AuthResponseDto> RefreshTokenAsync(
        string refreshToken,
        CancellationToken ct = default);
    
    Task LogoutAsync(
        string userId,
        CancellationToken ct = default);
}