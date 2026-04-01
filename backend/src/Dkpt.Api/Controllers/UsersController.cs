using Dkpt.Application.Common.DTOs;
using Dkpt.Application.Common.Interfaces;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dkpt.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepo;
    private readonly IAuthService _authService;

    public UsersController(IUserRepository userRepo, IAuthService authService)
    {
        _userRepo = userRepo;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var users = await _userRepo.GetAllAsync(ct);
        var dtos = users.Select(u => new UserDto(u.Id, u.Email, u.Role.ToString(), u.CreatedAt));
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest req, CancellationToken ct)
    {
        var existing = await _userRepo.GetByEmailAsync(req.Email, ct);
        if (existing is not null)
            return Conflict(new { message = "Cet email est déjà utilisé." });

        var user = new User
        {
            Email = req.Email,
            PasswordHash = _authService.HashPassword(req.Password),
            Role = req.Role
        };

        await _userRepo.AddAsync(user, ct);
        return Created("", new UserDto(user.Id, user.Email, user.Role.ToString(), user.CreatedAt));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest req, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(id, ct);
        if (user is null) return NotFound();

        if (req.Email is not null) user.Email = req.Email;
        if (req.Role.HasValue) user.Role = req.Role.Value;
        if (!string.IsNullOrEmpty(req.Password)) user.PasswordHash = _authService.HashPassword(req.Password);

        await _userRepo.UpdateAsync(user, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _userRepo.DeleteAsync(id, ct);
        return NoContent();
    }
}
