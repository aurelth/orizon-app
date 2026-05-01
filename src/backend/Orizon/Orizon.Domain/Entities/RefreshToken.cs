using Orizon.Domain.Common;

namespace Orizon.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } =
        Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
    public string UserId { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; } =
        DateTime.UtcNow.AddDays(7);

    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
    }
}