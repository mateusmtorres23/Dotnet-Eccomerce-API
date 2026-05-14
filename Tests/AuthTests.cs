using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Domain;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Services;

namespace Tests;

public class AuthTests
{
    private readonly ITokenService _tokenService;
    private const string TestKey = "VIRVLIfI7xLzWDAL6y9ShGIt8J5q1G/XLF206n0ySq4=";

    public AuthTests()
    {
        var myConfiguration = new Dictionary<string, string?>
        {
            {"JwtSettings:Key", TestKey},
            {"JwtSettings:Issuer", "test-issuer"},
            {"JwtSettings:Audience", "test-audience"}
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        _tokenService = new TokenService(configuration);
    }

    [Fact]
    public void TestGenerateToken()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "shannon@test.com",
            Password = "hashed_password",
            Role = UserRole.Admin
        };

        var token = _tokenService.GenerateToken(user);

        token.Should().NotBeNullOrEmpty();
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(token);

        jsonToken.Issuer.Should().Be("test-issuer");
        jsonToken.Audiences.Should().Contain("test-audience");
        
        jsonToken.Claims.Should().Contain(c => c.Type.Contains("email") && c.Value == user.Email);
        jsonToken.Claims.Should().Contain(c => c.Type.Contains("role") && c.Value == UserRole.Admin.ToString());
    }
}