using Dkpt.Application.Common.Interfaces;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Interfaces;

namespace Dkpt.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, ct);
        if (user is null) return null;

        return VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
