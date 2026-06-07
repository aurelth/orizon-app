namespace Orizon.API.Requests.Users;

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);