using Tests.Fixture;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Domain.DTOs.Cart;
using Domain.Models;
using Services;


namespace Tests.Service;

public class CartServiceTests : IntegrationTestBase
{
    private readonly CartService _cartService;
    public CartServiceTests(DatabaseFixture databaseFixture) : base(databaseFixture)
    {
        _cartService = new CartService(DbContext);
    }

    [Fact]
    public async Task AddProduct_ProductNotfound()
    {
        var request = new AddProductCartRequest(Guid.NewGuid());
        
        Func<Task> act = async () => await _cartService.AddProductCart(request, Guid.NewGuid());
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Product not found");
    }

    [Fact]
    public async Task AddProduct_ReturnCartItem()
    {
        await ResetDatabaseAsync();
        
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@email.com",
            Password = "password",
            Role = UserRole.Customer
        };

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "password",
            Role = UserRole.Seller
        };
        
        DbContext.Users.AddRange(user, owner);
        await DbContext.SaveChangesAsync();
        
        var store = new Store()
        {
            Id = Guid.NewGuid(),
            Name = "Test Store",
            OwnerId = owner.Id
        };
        
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test Description",
            Price = 100,
            StoreId = store.Id
        };
        
        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product);
        await DbContext.SaveChangesAsync();
        
        var result = await _cartService.AddProductCart(new AddProductCartRequest(product.Id), user.Id);
        
        result.Should().NotBeNull();
        result.ProductName.Should().Be(product.Name);
        result.ProductPrice.Should().Be(product.Price);
    }

    [Fact]
    public async Task RemoveProduct_ProductNotfound()
    {
        var productId = Guid.NewGuid();
        
        Func<Task> act = async () => await _cartService.RemoveProductCart(
            new RemoveProductCartRequest(productId), Guid.NewGuid()
            );
        
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Product not found or not in cart");
    }

    [Fact]
    public async Task RemoveProduct_RemoveCartItem()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@email.com",
            Password = "password",
            Role = UserRole.Customer
        };

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "password",
            Role = UserRole.Seller
        };
        
        DbContext.Users.AddRange(user, owner);
        await DbContext.SaveChangesAsync();
        
        var store = new Store()
        {
            Id = Guid.NewGuid(),
            Name = "Test Store",
            OwnerId = owner.Id
        };
        
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Description = "Test Description",
            Price = 100,
            StoreId = store.Id
        };
        
        DbContext.Stores.AddRange(store);
        DbContext.Products.AddRange(product);

        var cartItem = new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ProductId = product.Id
        };
        
        DbContext.CartItems.AddRange(cartItem);
        
        await DbContext.SaveChangesAsync();
        
        await _cartService.RemoveProductCart(new RemoveProductCartRequest(product.Id), user.Id);

        var result = await DbContext.CartItems
            .FirstOrDefaultAsync(ci => ci.ProductId == product.Id && ci.UserId == user.Id);
            
        result.Should().BeNull();
    }
}