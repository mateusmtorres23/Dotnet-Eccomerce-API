using FluentAssertions;
using Domain.Models;
using Services;
using Tests.Fixture;

namespace Tests.Service.ProductTests;

public class ReadProductTests : IntegrationTestBase
{
    private readonly ProductService _productService;

    public ReadProductTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        _productService = new ProductService(DbContext);
    }

    [Fact]
    public async Task ReadProducts_StoreDoesNotExist()
    {
        await ResetDatabaseAsync();
        
        var storeId = Guid.NewGuid();
        Func<Task> act = async () => await _productService.ReadStoreProduct(storeId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("This store does not exist.");
    }

    [Fact]
    public async Task ReadProducts_ReturnStoreProducts()
    {
        await ResetDatabaseAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };
        
        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = owner.Id
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Description = "Description",
            Name = "Product A",
            Price = 100,
            StoreId = store.Id
        };

        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product);
        await DbContext.SaveChangesAsync();

        var result = await _productService.ReadStoreProduct(store.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Product A");
    }

    [Fact]
    public async Task ReadProductDetails_ProductDoesNotExist()
    {
        await ResetDatabaseAsync();
        
        var productId = Guid.NewGuid();
        Func<Task> act = async () => await _productService.ReadProductDetails(productId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("This product does not exist.");
    }

    [Fact]
    public async Task ReadProductDetails_ReturnProductDetails()
    {
        await ResetDatabaseAsync();

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "test_password",
            Role = UserRole.Seller
        };
        
        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store A",
            OwnerId = owner.Id
        };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Description = "Description",
            Name = "Product A",
            Price = 100,
            StoreId = store.Id
        };

        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product);
        await DbContext.SaveChangesAsync();

        var result = await _productService.ReadProductDetails(product.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("Product A");
        result.Description.Should().Be("Description");
        result.Price.Should().Be(100);
    }
}