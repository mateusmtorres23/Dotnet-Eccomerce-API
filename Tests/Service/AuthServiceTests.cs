using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Domain.Models;
using Domain.DTOs.Auth;
using Domain.Exceptions;
using Infra;
using Services;

namespace Tests.Service;

public class AuthServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ecommerce_testdb")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();
    
    private  AppDbContext _dbContext = null!;
    private  ITokenService _tokenService = null!;
    private  PasswordHasher _passwordHasher = null!;
    private  AuthService _authService = null!;
    
    private const string TestKey = "VIRVLIfI7xLzWDAL6y9ShGIt8J5q1G/XLF206n0ySq4=";

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;
        
        _dbContext = new AppDbContext(options);
        await _dbContext.Database.MigrateAsync();

        var myConfiguration = new Dictionary<string, string>()
        {
            {"Jwt:Key", TestKey},
            {"Jwt:Issuer", "test-issuer"},
            {"Jwt:Audience", "test-audience"},
            {"Jwt:ExpireMinutes", "60"}
        };
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(myConfiguration)
            .Build();
        
        _tokenService = new TokenService(configuration);
        _passwordHasher = new PasswordHasher();
        
        _authService = new AuthService(_dbContext, _passwordHasher, _tokenService);
    }
    
    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
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
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com",
            Password = "dummy_hash",
            Role = UserRole.Customer
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var request = new RegisterRequest("existing@test.com", "password123");

        Func<Task> act = async () => await _authService.RegisterUser(request);

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Fact]
    public async Task RegisterUser_CreateUser()
    {
        var request = new RegisterRequest("newuser@test.com", "password123");

        var result = await _authService.RegisterUser(request);

        result.Should().NotBeNull();

        var savedUser = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(UserRole.Customer);
        _passwordHasher.Verify(request.Password, savedUser.Password).Should().BeTrue();
    }

    [Fact]
    public async Task LoginUser_EmailDoesNotExist()
    {
        var request = new LoginRequest("nonexistent@test.com", "password123");

        Func<Task> act = async () => await _authService.LoginUser(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginUser_InvalidPassword()
    {
        var request = new LoginRequest("user@test.com", "wrongpassword");
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = _passwordHasher.Hash("correctpassword"),
            Role = UserRole.Customer
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        Func<Task> act = async () => await _authService.LoginUser(request);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid email or password.");
    }

    [Fact]
    public async Task LoginUser_ReturnToken()
    {
        var rawPassword = "validpassword";
        var request = new LoginRequest("validuser@test.com", rawPassword);
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Password = _passwordHasher.Hash(rawPassword),
            Role = UserRole.Customer
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var result = await _authService.LoginUser(request);

        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
    }
}