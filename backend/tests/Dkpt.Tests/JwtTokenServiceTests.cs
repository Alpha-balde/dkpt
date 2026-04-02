using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dkpt.Domain.Entities;
using Dkpt.Domain.Enums;
using Dkpt.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Dkpt.Tests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _tokenService;

    public JwtTokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "TestSecretKey_MustBeAtLeast32Characters!",
                ["Jwt:Issuer"] = "dkpt-api-test",
                ["Jwt:Audience"] = "dkpt-client-test"
            })
            .Build();

        _tokenService = new JwtTokenService(config);
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwt()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@dkpt.com",
            Role = UserRole.Admin
        };

        // Act
        var token = _tokenService.GenerateToken(user);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Should be a valid JWT (3 parts separated by dots)
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "sg@dkpt.com",
            Role = UserRole.Secretaire
        };

        // Act
        var token = _tokenService.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal("sg@dkpt.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Secretaire", jwt.Claims.First(c => c.Type == "role").Value);
        Assert.Equal(userId.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("dkpt-api-test", jwt.Issuer);
    }
}
