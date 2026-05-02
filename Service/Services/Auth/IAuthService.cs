using Service.DTOs.AuthDTOs;

namespace Service.Services.Auth;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<string?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<bool> UpdatePasswordAsync(string email, UpdatePasswordRequest request, CancellationToken ct = default);
}
