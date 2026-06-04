using FluentAssertions;
using Domain.Models;
using Domain.Exceptions;
using Domain.DTOs.Store;
using Tests.Fixture;
using Services;

namespace Tests.Service.StoreTests;

public class StoreServiceTests : IntegrationTestBase
{
    private readonly StoreService _storeService;

    public StoreServiceTests(DatabaseFixture fixture) : base(fixture)
    {
        _storeService = new StoreService(DbContext);
    }
    
    [Fact]
    public async Task UpdateStore_UserNotFound()
    {
        await ResetDatabaseAsync();

        var request = new UpdateStoreRequest(Guid.NewGuid(), "NewName");

        Func<Task> act = async () => await _storeService.UpdateStore(Guid.NewGuid(), request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task UpdateStore_StoreNotFound()
    {
        await ResetDatabaseAsync();

        var seller = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };
        DbContext.Users.Add(seller);
        await DbContext.SaveChangesAsync();

        var request = new UpdateStoreRequest(Guid.NewGuid(), "NewName");

        Func<Task> act = async () => await _storeService.UpdateStore(seller.Id, request);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Store with this ID not found.");
    }

    [Fact]
    public async Task UpdateStore_SellerNotOwner()
    {
        await ResetDatabaseAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };
        var otherSeller = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };
        DbContext.Users.AddRange(owner, otherSeller);

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store",
            OwnerId = owner.Id
        };
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var request = new UpdateStoreRequest(store.Id, "NewName");

        Func<Task> act = async () => await _storeService.UpdateStore(otherSeller.Id, request);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to perform this action.");
    }

    [Fact]
    public async Task UpdateStore_DuplicateNameForOwner()
    {
        await ResetDatabaseAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner9@example.com",
            Password = "pwd",
            Role = UserRole.Seller
        };
        DbContext.Users.Add(owner);

        var store1 = new Store
        {
            Id = Guid.NewGuid(),
            Name = "StoreA",
            OwnerId = owner.Id
        };
        var store2 = new Store
        {
            Id = Guid.NewGuid(),
            Name = "StoreB",
            OwnerId = owner.Id
        };
        DbContext.Stores.AddRange(store1, store2);
        await DbContext.SaveChangesAsync();

        var request = new UpdateStoreRequest(store2.Id, "StoreA"); // duplicate name for same owner

        Func<Task> act = async () => await _storeService.UpdateStore(owner.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("User already owns a store with this name.");
    }

    [Fact]
    public async Task UpdateStore_Success()
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

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "OldName",
            OwnerId = owner.Id
        };
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        var request = new UpdateStoreRequest(store.Id, "NewName");

        var response = await _storeService.UpdateStore(owner.Id, request);

        response.Name.Should().Be("NewName");

        var refreshed = await DbContext.Stores.FindAsync(store.Id);
        refreshed!.Name.Should().Be("NewName");
    }
}
