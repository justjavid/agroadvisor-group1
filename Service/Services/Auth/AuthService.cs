using Domain.Models.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repository.Repositories.Interfaces;
using Service.DTOs.AuthDTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Service.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IConfiguration config)
    {
        _users = users;
        _config = config;
    }

    public async Task<bool> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var existing = await _users.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            return false;

        var user = new User
        {
            Name = request.Name,
            Surname = request.Surname,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        await _users.AddAsync(user, ct);
        return true;
    }

    public async Task<string?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return null;

        return GenerateToken(user);
    }

    public async Task<bool> UpdatePasswordAsync(string email, UpdatePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null)
            return false;

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _users.UpdateAsync(user, ct);
        return true;
    }

    private string GenerateToken(User user)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        var issuer = _config["Jwt:Issuer"] ?? "AgroAdvisor";
        var audience = _config["Jwt:Audience"] ?? "AgroAdvisor";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
