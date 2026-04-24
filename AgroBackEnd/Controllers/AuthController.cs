using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service;
using System.Security.Claims;

namespace AgroBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var success = await _authService.RegisterAsync(request);
        if (!success)
            return BadRequest("Email already in use.");

        return Ok("Registered successfully.");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await _authService.LoginAsync(request);
        if (token == null)
            return Unauthorized("Invalid email or password.");

        return Ok(new { token });
    }

    [Authorize]
    [HttpPut("update-password")]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordRequest request)
    {
        // Get email from JWT token — no need to ask user to send it
        var email = User.FindFirstValue(ClaimTypes.Email);
        if (email == null)
            return Unauthorized();

        var success = await _authService.UpdatePasswordAsync(email, request);
        if (!success)
            return BadRequest("Current password is incorrect.");

        return Ok("Password updated successfully.");
    }
}