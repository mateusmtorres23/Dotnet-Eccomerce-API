using FluentAssertions;
using Domain.Models;
using Domain.DTOs.Product;
using Domain.Exceptions;
using Services;
using Tests.Fixture;

namespace Tests.Service.ProductTests;

public class UpdateProductTests : IntegrationTestBase
{
    private readonly ProductService _productService;

    public UpdateProductTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        _productService = new ProductService(DbContext);
    }

    [Fact]
    public async Task UpdateProduct_UserNotFound()
    {
        await ResetDatabaseAsync();

        var productId = Guid.NewGuid();
        var request = new UpdateProductRequest("Updated Product", "Updated Description", 150);
        Func<Task> act = async () => await _productService.UpdateProduct(request, Guid.NewGuid(), productId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task UpdateProduct_UserIsCustomer()
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
        var request = new UpdateProductRequest("Updated Product", "Updated Description", 150);
        Func<Task> act = async () => await _productService.UpdateProduct(request, user.Id, productId);

        await act.Should().ThrowAsync<UnauthorizedUserException>();
    }

    [Fact]
    public async Task UpdateProduct_ProductDoesNotExist()
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

        var request = new UpdateProductRequest("Updated Product", "Updated Description", 150);
        Func<Task> act = async () => await _productService.UpdateProduct(request, user.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Product not found.");
    }

    [Fact]
    public async Task UpdateProduct_UserIsSellerButNotStoreOwner()
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

        var request = new UpdateProductRequest("Updated Product", "Updated Description", 150);
        Func<Task> act = async () => await _productService.UpdateProduct(request, seller.Id, product.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to perform this action.");
    }

    [Fact]
    public async Task UpdateProduct_ProductWithSameNameExistsInStore()
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

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = user.Id
        };

        var product1 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product A",
            Description = "Description",
            Price = 100,
            StoreId = store.Id
        };

        var product2 = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product B",
            Description = "Description",
            Price = 100,
            StoreId = store.Id
        };

        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product1, product2);
        await DbContext.SaveChangesAsync();

        var request = new UpdateProductRequest("Product A", "Updated Description", 150);
        Func<Task> act = async () => await _productService.UpdateProduct(request, user.Id, product2.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A product with this name already exists in this store.");
    }

    [Fact]
    public async Task UpdateProduct_SuccessAsAdmin()
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
            Description = "Original Description",
            Price = 100,
            StoreId = store.Id
        };

        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product);
        await DbContext.SaveChangesAsync();

        var request = new UpdateProductRequest("Updated Product", "Updated Description", 150);
        var result = await _productService.UpdateProduct(request, admin.Id, product.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Product");
        result.Price.Should().Be(150);
        result.Description.Should().Be("Updated Description");

        var updatedProduct = await DbContext.Products.FindAsync(product.Id);
        updatedProduct.Name.Should().Be("Updated Product");
        updatedProduct.Price.Should().Be(150);
    }

    [Fact]
    public async Task UpdateProduct_SuccessAsSellerAndOwner()
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
            Description = "Original Description",
            Price = 100,
            StoreId = store.Id
        };

        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product);
        await DbContext.SaveChangesAsync();

        var request = new UpdateProductRequest("Updated Product", "Updated Description", 200);
        var result = await _productService.UpdateProduct(request, user.Id, product.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Product");
        result.Price.Should().Be(200);
    }
}