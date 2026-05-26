using FluentAssertions;
using Domain.Models;
using Domain.Exceptions;
using Services;
using Tests.Fixture;

namespace Tests.Service.ProductTests;

public class DeleteProductTests : IntegrationTestBase
{
    private readonly ProductService _productService;

    public DeleteProductTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        _productService = new ProductService(DbContext);
    }

    [Fact]
    public async Task DeleteProduct_UserNotFound()
    {
        await ResetDatabaseAsync();
        
        var productId = Guid.NewGuid();
        Func<Task> act = async () => await _productService.DeleteProduct(productId, Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task DeleteProduct_UserIsCustomer()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@email.com",
            Password = "test_password",
            Role = UserRole.Customer
        };

        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        var productId = Guid.NewGuid();
        Func<Task> act = async () => await _productService.DeleteProduct(productId, user.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>();
    }

    [Fact]
    public async Task DeleteProduct_ProductDoesNotExist()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };

        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _productService.DeleteProduct(Guid.NewGuid(), user.Id);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Product not found.");
    }

    [Fact]
    public async Task DeleteProduct_UserIsSellerButNotStoreOwner()
    {
        await ResetDatabaseAsync();
        
        var seller = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };

        var storeOwner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(seller, storeOwner);
        await DbContext.SaveChangesAsync();

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = storeOwner.Id
        };

        var product = new Product
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

        Func<Task> act = async () => await _productService.DeleteProduct(product.Id, seller.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to perform this action.");
    }

    [Fact]
    public async Task DeleteProduct_SuccessAsAdmin()
    {
        await ResetDatabaseAsync();
        
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@email.com",
            Password = "test_password",
            Role = UserRole.Admin
        };

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(admin, owner);
        await DbContext.SaveChangesAsync();

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = owner.Id
        };

        var product = new Product
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

        await _productService.DeleteProduct(product.Id, admin.Id);

        var deletedProduct = await DbContext.Products.FindAsync(product.Id);
        deletedProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProduct_SuccessAsSellerAndOwner()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(user);
        await DbContext.SaveChangesAsync();

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = user.Id
        };

        var product = new Product
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

        await _productService.DeleteProduct(product.Id, user.Id);

        var deletedProduct = await DbContext.Products.FindAsync(product.Id);
        deletedProduct.Should().BeNull();
    }
}