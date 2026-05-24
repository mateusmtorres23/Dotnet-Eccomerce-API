using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
using Tests.Fixture;

namespace Tests;

public class AppDbContextTests : IntegrationTestBase
{
    public AppDbContextTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
    }

    [Fact]
    public async Task CheckDuplicityException()
    {
        await ResetDatabaseAsync();

        User testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "testemail@email.com",
            Password = "test_password",
            Role = UserRole.Customer,
        };

        Store testStore = new Store
        {
            Id = Guid.NewGuid(),
            Name = "TestStore",
            OwnerId = testUser.Id,
        };

        Product testProduct1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "TestProduct",
            Description = "TestProductDescription",
            Price = 100,
            StoreId = testStore.Id
        };

        Product testProduct2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "TestProduct",
            Description = "TestProductDescription2",
            Price = 200,
            StoreId = testStore.Id
        };

        DbContext.Users.Add(testUser);
        DbContext.Stores.Add(testStore);
        DbContext.Products.Add(testProduct1);
        DbContext.Products.Add(testProduct2);

        Func<Task> act = async () => await DbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task CheckCascadeDeletion()
    {
        await ResetDatabaseAsync();

        User testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "testemail@email.com",
            Password = "test_password",
            Role = UserRole.Customer,
        };

        Store testStore = new Store
        {
            Id = Guid.NewGuid(),
            Name = "TestStore",
            OwnerId = testUser.Id,
        };

        Product testProduct = new Product
        {
            Id = Guid.NewGuid(),
            Name = "TestProduct",
            Description = "TestProductDescription",
            Price = 100,
            StoreId = testStore.Id
        };

        DbContext.Users.Add(testUser);
        DbContext.Stores.Add(testStore);
        DbContext.Products.Add(testProduct);

        await DbContext.SaveChangesAsync();

        DbContext.ChangeTracker.Clear();

        var storeToDelete = await DbContext.Stores.FindAsync(testStore.Id);

        DbContext.Stores.Remove(storeToDelete!);
        await DbContext.SaveChangesAsync();

        var productStillExists = await DbContext.Products.AnyAsync(p => p.Id == testProduct.Id);

        productStillExists.Should().BeFalse();
    }
}