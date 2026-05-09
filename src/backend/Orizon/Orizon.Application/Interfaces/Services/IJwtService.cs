using Orizon.Domain.Entities;

namespace Orizon.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateToken(AppUser user);
    string GenerateServiceToken();
    bool ValidateToken(string token);
    Guid? GetUserIdFromToken(string token);
}