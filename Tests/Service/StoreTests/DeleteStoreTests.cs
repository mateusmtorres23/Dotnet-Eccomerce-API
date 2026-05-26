using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Domain.Exceptions;
using Tests.Fixture;
using Services;

namespace Tests.Service.StoreTests;

public class DeleteStoreTests : IntegrationTestBase
{
    private readonly StoreService _storeService;

    public DeleteStoreTests(DatabaseFixture fixture) : base(fixture)
    {
        _storeService = new StoreService(DbContext);
    }
    
    [Fact]
    public async Task DeleteStore_UserNotFound()
    {
        await ResetDatabaseAsync();

        Func<Task> act = async () => await _storeService.DeleteStore(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task DeleteStore_UserIsCustomer()
    {
        await ResetDatabaseAsync();

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = "cust@email.com",
            Password = "pwd",
            Role = UserRole.Customer
        };
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "StoreToDelete",
            OwnerId = Guid.NewGuid()
        };
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _storeService.DeleteStore(store.Id, customer.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to perform this action.");
    }

    [Fact]
    public async Task DeleteStore_StoreNotFound()
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

        Func<Task> act = async () => await _storeService.DeleteStore(Guid.NewGuid(), seller.Id);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Store  not found.");
    }

    [Fact]
    public async Task DeleteStore_UserNotOwner_ThrowsUnauthorized()
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
            Email = "seller@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(owner, otherSeller);

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "OwnerStore6",
            OwnerId = owner.Id
        };
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _storeService.DeleteStore(store.Id, otherSeller.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to perform this action.");
    }

    [Fact]
    public async Task DeleteStore_OwnerDeletes_Success()
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
            Name = "StoreToBeDeleted",
            OwnerId = owner.Id
        };
        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        await _storeService.DeleteStore(store.Id, owner.Id);

        var exists = await DbContext.Stores.AnyAsync(s => s.Id == store.Id);
        exists.Should().BeFalse();
    }
    
}