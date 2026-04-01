using Dkpt.Domain.Entities;

namespace Dkpt.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
