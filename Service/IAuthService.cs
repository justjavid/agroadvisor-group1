using Domain;

namespace Service;

public interface IAuthService
{
    Task<string?> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<bool> UpdatePasswordAsync(string email, UpdatePasswordRequest request);
}