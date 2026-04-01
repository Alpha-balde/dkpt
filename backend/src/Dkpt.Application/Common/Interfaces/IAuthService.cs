using Dkpt.Domain.Entities;

namespace Dkpt.Application.Common.Interfaces;

public interface IAuthService
{
    Task<User?> ValidateCredentialsAsync(string email, string password, CancellationToken ct = default);
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}
