using FluentAssertions;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Infra;
using Services;
using Domain.Models;
using Domain.DTOs.User;
namespace Tests.Service;

public class UserServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ecommerce_testdb")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();
    private AppDbContext _dbContext;
    private UserService _userService;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;
        
        _dbContext = new AppDbContext(options);
        
        await _dbContext.Database.MigrateAsync();
        _userService = new UserService(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task ListUsers_UserDoesNotExists()
    {
        var NonExistentUserId = Guid.NewGuid();
        
        Func<Task> act = async () => await _userService.ListUsers(NonExistentUserId);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task ListUsers_UserIsNotAdmin()
    {
        var nonAdminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Customer
        };

        Func<Task> act = async () => await _userService.ListUsers(nonAdminUser.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("This user is not authorized to view this information");
    }

    [Fact]
    public async Task ListUsers_ReturnListOfUsers()
    {
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };

        var standardUser = new User()
        {
            Id = Guid.NewGuid(),
            Email = "standard@email.com",
            Password = "standard_password",
            Role = UserRole.Customer
        };
        
        _dbContext.Users.AddRange(adminUser, standardUser);
        await _dbContext.SaveChangesAsync();
        
        var result = await _userService.ListUsers(adminUser.Id);
        
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Id == adminUser.Id && u.Email == adminUser.Email);
        result.Should().Contain(u => u.Id == standardUser.Id && u.Email == standardUser.Email);
    }

    [Fact]
    public async Task GetUserDetails_UserDoesNotExists()
    {
        var viewUserId = Guid.NewGuid();
        var nonExistentUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };

        Func<Task> act = async () => await _userService.GetUserDetails(nonExistentUser.Id, viewUserId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task GetUserDetails_UserIsNotAdmin()
    {
        var nonAdminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Customer
        };
        
        var standardUser = new User()
        {
            Id = Guid.NewGuid(),
            Email = "standard@email.com",
            Password = "standard_password",
            Role = UserRole.Customer
        };
        
        _dbContext.Users.AddRange(nonAdminUser, standardUser);
        await _dbContext.SaveChangesAsync();

        Func<Task> act = async () => await _userService.GetUserDetails(nonAdminUser.Id, standardUser.Id);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("This user is not authorized to view this information");
    }

    [Fact]
    public async Task GetUserDetails_TargetUserDoesNotExists()
    {
        var nonAdminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Customer
        };

        var targetUserId = Guid.NewGuid();
        
        _dbContext.Users.AddRange(nonAdminUser);
        await _dbContext.SaveChangesAsync();

        Func<Task> act = async () => await _userService.GetUserDetails(nonAdminUser.Id, targetUserId);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("This user is not authorized to view this information");
    }

    [Fact]
    public async Task GetUserDetails_ReturnUserDetails()
    {
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };
        
        var standardUser = new User()
        {
            Id = Guid.NewGuid(),
            Email = "standard@email.com",
            Password = "standard_password",
            Role = UserRole.Customer
        };
        
        _dbContext.Users.AddRange(adminUser, standardUser);
        await _dbContext.SaveChangesAsync();

        var result = await _userService.GetUserDetails(adminUser.Id, standardUser.Id);
        
        result.Should().NotBeNull();
        result.Id.Should().Be(standardUser.Id);
        result.Email.Should().Be(standardUser.Email);
        result.Role.Should().Be(UserRole.Customer);
        result.Stores.Should().NotBeNull();
        result.Stores.Should().BeEmpty();
    }

    [Fact]
    public async Task UpgradeRole_TargetUserDoesNotExists()
    {
        var userId =  Guid.NewGuid();
        var request = new UpgradeRoleRequest(userId, UserRole.Admin);
        
        Func<Task> act = async () => await _userService.UpgradeRole(request);
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }
    
    [Fact]
    public async Task UpgradeRole_UpgradeUserRole()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email",
            Password = "test_password",
            Role = UserRole.Customer
        };
        _dbContext.Users.AddRange(user);
        await _dbContext.SaveChangesAsync();

        var request = new UpgradeRoleRequest(user.Id, UserRole.Admin);
        var result = await _userService.UpgradeRole(request);

        result.Should().NotBeNull();
        result.Email.Should().Be(user.Email);
        result.Role.Should().Be(UserRole.Admin);
    }
        
        
}