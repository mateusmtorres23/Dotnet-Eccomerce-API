using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Infra;
using Services;
using Domain.Models;
using Domain.DTOs.User;
using Tests.Fixture;

namespace Tests.Service;

public class UserServiceTests : IntegrationTestBase
{
    private readonly UserService _userService;

    public UserServiceTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        _userService = new UserService(DbContext);
    }

    [Fact]
    public async Task ListUsers_ReturnListOfUsers()
    {
        await ResetDatabaseAsync();
        
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

        DbContext.Users.AddRange(adminUser, standardUser);
        await DbContext.SaveChangesAsync();

        var result = await _userService.ListUsers(adminUser.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Id == adminUser.Id && u.Email == adminUser.Email);
        result.Should().Contain(u => u.Id == standardUser.Id && u.Email == standardUser.Email);
    }

    [Fact]
    public async Task GetUserDetails_UserDoesNotExists()
    {
        await ResetDatabaseAsync();
        
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
    public async Task GetUserDetails_TargetUserDoesNotExists()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };

        var targetUserId = Guid.NewGuid();

        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _userService.GetUserDetails(user.Id, targetUserId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task GetUserDetails_ReturnUserDetails()
    {
        await ResetDatabaseAsync();
        
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

        DbContext.Users.AddRange(adminUser, standardUser);
        await DbContext.SaveChangesAsync();

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
        await ResetDatabaseAsync();
        
        var userId = Guid.NewGuid();
        var request = new UpgradeRoleRequest(userId, UserRole.Admin);

        Func<Task> act = async () => await _userService.UpgradeRole(request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task UpgradeRole_UpgradeUserRole()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email",
            Password = "test_password",
            Role = UserRole.Customer
        };
        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        var request = new UpgradeRoleRequest(user.Id, UserRole.Admin);
        var result = await _userService.UpgradeRole(request);

        result.Should().NotBeNull();
        result.Email.Should().Be(user.Email);
        result.Role.Should().Be(UserRole.Admin);
    }
}