using Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Service;

public interface IAuthService
{
    Task<string?> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
}
