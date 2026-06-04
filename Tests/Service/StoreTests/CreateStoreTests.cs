using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Domain.Exceptions;
using Domain.DTOs.Store;
using Tests.Fixture;
using Services;

namespace Tests.Service.StoreTests;

public class CreateStoreTests: IntegrationTestBase
{
    private readonly StoreService _storeService;

    public CreateStoreTests(DatabaseFixture fixture) : base(fixture)
    {
        _storeService = new StoreService(DbContext);
    }
    
    [Fact]
    public async Task CreateStore_UserNotFound()
    {
        await ResetDatabaseAsync();

        var request = new CreateStoreRequest("NewStore");

        Func<Task> act = async () => await _storeService.CreateStore(request, Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found");
    }

    [Fact]
    public async Task CreateStore_DuplicateName()
    {
        await ResetDatabaseAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };
        DbContext.Users.Add(owner);

        var existingStore = new Store
        {
            Id = Guid.NewGuid(),
            Name = "DuplicateStore",
            OwnerId = owner.Id
        };
        DbContext.Stores.Add(existingStore);

        await DbContext.SaveChangesAsync();

        var request = new CreateStoreRequest("DuplicateStore");

        Func<Task> act = async () => await _storeService.CreateStore(request, owner.Id);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("A store with this name already exists.");
    }

    [Fact]
    public async Task CreateStore_Success()
    {
        await ResetDatabaseAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };
        DbContext.Users.Add(owner);
        await DbContext.SaveChangesAsync();

        var request = new CreateStoreRequest("BrandNewStore");

        var response = await _storeService.CreateStore(request, owner.Id);

        response.Name.Should().Be("BrandNewStore");
        response.OwnerEmail.Should().Be(owner.Email);

        var exists = await DbContext.Stores.AnyAsync(s => s.Name == "BrandNewStore" && s.OwnerId == owner.Id);
        exists.Should().BeTrue();
    }
}