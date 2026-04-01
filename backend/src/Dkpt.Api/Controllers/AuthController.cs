using Dkpt.Application.Common.DTOs;
using Dkpt.Application.Common.Interfaces;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Dkpt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;
    private readonly IUserRepository _userRepository;

    public AuthController(
        IAuthService authService,
        ITokenService tokenService,
        IUserRepository userRepository)
    {
        _authService = authService;
        _tokenService = tokenService;
        _userRepository = userRepository;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _authService.ValidateCredentialsAsync(request.Email, request.Password, ct);
        if (user is null)
            return Unauthorized(new { message = "Email ou mot de passe incorrect." });

        var token = _tokenService.GenerateToken(user);

        return Ok(new LoginResponse(token, user.Email, user.Role.ToString()));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, ct);
        if (existing is not null)
            return Conflict(new { message = "Cet email est déjà utilisé." });

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _authService.HashPassword(request.Password),
            Role = request.Role
        };

        await _userRepository.AddAsync(user, ct);

        var token = _tokenService.GenerateToken(user);
        return Created("", new LoginResponse(token, user.Email, user.Role.ToString()));
    }
}
