using Dkpt.Domain.Common;
using Dkpt.Domain.Enums;

namespace Dkpt.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Lecteur;
}
