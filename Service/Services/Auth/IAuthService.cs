using Service.DTOs.AuthDTOs;

namespace Service.Services.Auth;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<string> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
