using Post34.DTOs;

namespace Post34.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
}
