using Dkpt.Infrastructure.Services;

namespace Dkpt.Tests;

public class AuthServiceTests
{
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        // AuthService needs IUserRepository, but HashPassword/VerifyPassword
        // don't use it, so we pass null for unit tests on those methods.
        _authService = new AuthService(null!);
    }

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "Test_P@ssw0rd_Unit";

        // Act
        var hash = _authService.HashPassword(password);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash); // Hash should differ from plain text
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        // Arrange
        var password = "Test_P@ssw0rd_Unit";
        var hash = _authService.HashPassword(password);

        // Act
        var result = _authService.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        // Arrange
        var password = "Test_P@ssw0rd_Unit";
        var hash = _authService.HashPassword(password);

        // Act
        var result = _authService.VerifyPassword("WrongPassword!", hash);

        // Assert
        Assert.False(result);
    }
}
