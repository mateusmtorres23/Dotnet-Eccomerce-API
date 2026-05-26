using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Domain.DTOs.Auth;
using Domain.Exceptions;
 using Services;
using Tests.Fixture;

namespace Tests.Service;

public class AuthServiceTests : IntegrationTestBase
{
    private readonly ITokenService _tokenService;
    private readonly PasswordHasher _passwordHasher;
    private readonly AuthService _authService;

    private const string TestKey = "VIRVLIfI7xLzWDAL6y9ShGIt8J5q1G/XLF206n0ySq4=";

    public AuthServiceTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        var myConfiguration = new Dictionary<string, string>()
        {
            {"Jwt:Key", TestKey},
            {"Jwt:Issuer", "test-issuer"},
            {"Jwt:Audience", "test-audience"},
            {"Jwt:ExpireMinutes", "60"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();

        _tokenService = new TokenService(configuration);
        _passwordHasher = new PasswordHasher();
        _authService = new AuthService(DbContext, _passwordHasher, _tokenService);
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

    [Fact]
    public async Task RegisterUser_EmailAlreadyExists()
    {
        await ResetDatabaseAsync();
        
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com",
            Password = "dummy_hash",
            Role = UserRole.Customer
        };
        DbContext.Users.Add(existingUser);
        await DbContext.SaveChangesAsync();

        var request = new RegisterRequest("existing@test.com", "password123");

        Func<Task> act = async () => await _authService.RegisterUser(request);

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Fact]
    public async Task RegisterUser_CreateUser()
    {
        await ResetDatabaseAsync();
        
        var request = new RegisterRequest("newuser@test.com", "password123");

        var result = await _authService.RegisterUser(request);

        result.Should().NotBeNull();

        var savedUser = await DbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Customer);
        _passwordHasher.Verify(request.Password, savedUser.Password).Should().BeTrue();
    }

    [Fact]
    public async Task LoginUser_EmailDoesNotExist()
    {
        await ResetDatabaseAsync();
        
        var request = new LoginRequest("nonexistent@test.com", "password123");

        Func<Task> act = async () => await _authService.LoginUser(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginUser_InvalidPassword()
    {
        await ResetDatabaseAsync();
        
        var request = new LoginRequest("user@test.com", "wrongpassword");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = _passwordHasher.Hash("correctpassword"),
            Role = UserRole.Customer
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _authService.LoginUser(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginUser_ReturnToken()
    {
        await ResetDatabaseAsync();
        
        var rawPassword = "validpassword";
        var request = new LoginRequest("validuser@test.com", rawPassword);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = _passwordHasher.Hash(rawPassword),
            Role = UserRole.Customer
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var result = await _authService.LoginUser(request);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
    }
}