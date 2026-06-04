using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Domain.Models;
using Domain.DTOs.Product;
using Domain.Exceptions;
using Services;
using Tests.Fixture;

namespace Tests.Service.ProductTests;

public class CreateProductTests : IntegrationTestBase
{
    private readonly ProductService _productService;

    public CreateProductTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        _productService = new ProductService(DbContext);
    }

    [Fact]
    public async Task CreateProduct_UserIsSellerButNotStoreOwner()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };
        
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "owner_password",
            Role = UserRole.Seller
        };
        
        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = owner.Id
        };

        DbContext.Users.AddRange(user, owner);
        DbContext.Stores.AddRange(store);
        await DbContext.SaveChangesAsync();

        var request = new CreateProductRequest("Product A", "Description", 100, store.Id);
        Func<Task> act = async () => await _productService.CreateProduct(request, user.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to perform this action.");
    }

    [Fact]
    public async Task CreateProduct_StoreDoesNotExist()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };

        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        var request = new CreateProductRequest("Product A", "Description", 100, Guid.NewGuid());

        Func<Task> act = async () => await _productService.CreateProduct(request, user.Id);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Store not found.");
    }

    [Fact]
    public async Task CreateProduct_ProductWithSameNameExistsInStore()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };

        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = user.Id
        };

        var product = new Product()
        {
            Id = Guid.NewGuid(),
            Name = "Product A",
            Description = "Description",
            Price = 100,
            StoreId = store.Id
        };
        
        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product);
        await DbContext.SaveChangesAsync();

        var request = new CreateProductRequest("Product A", "Description", 100, store.Id);

        Func<Task> act = async () => await _productService.CreateProduct(request, user.Id);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("A product with this name already exists in this store.");
    }

    [Fact]
    public async Task CreateProduct_ReturnProduct_UserIsSellerAndOwner()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        var store = new Store()
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = user.Id
        };

        DbContext.Stores.AddRange(store);
        await DbContext.SaveChangesAsync();

        var request = new CreateProductRequest("Product A", "Description", 100, store.Id);

        var result = await _productService.CreateProduct(request, user.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Product A");
        result.Price.Should().Be(100);
    }

    [Fact]
    public async Task CreateProduct_ReturnProduct_UserIsAdmin()
    {
        await ResetDatabaseAsync();
        
        var admin = new User { Id = Guid.NewGuid(), Email = "admin@test.com", Password = "pwd", Role = UserRole.Admin };
        var owner = new User { Id = Guid.NewGuid(), Email = "owner@test.com", Password = "pwd", Role = UserRole.Seller };
        var store = new Store { Id = Guid.NewGuid(), Name = "Store", OwnerId = owner.Id };

        DbContext.Users.AddRange(admin, owner);
        DbContext.Stores.AddRange(store);
        await DbContext.SaveChangesAsync();

        var request = new CreateProductRequest("Product A", "Description", 100, store.Id);
        var result = await _productService.CreateProduct(request, admin.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Product A");
        result.Price.Should().Be(100);

        var savedProduct = await DbContext.Products.SingleOrDefaultAsync(p => p.StoreId == store.Id);
        savedProduct.Should().NotBeNull();
    }
}