using FluentAssertions;
using Domain.Models;
using Domain.Exceptions;
using Domain.DTOs.Store;
using Tests.Fixture;
using Services;

namespace Tests.Service.StoreTests;

public class ReadStoreTests : IntegrationTestBase
{
    private readonly StoreService _storeService;

    public ReadStoreTests(DatabaseFixture fixture) : base(fixture)
    {
        _storeService = new StoreService(DbContext);
    }

    [Fact]
    public async Task ListAllStores_UserNotFound()
    {
        Func<Task> act = async () => await _storeService.ListAllStores(Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task ListAllStores_UserNotAdmin()
    {
        await ResetDatabaseAsync();
        
        var notAdmin = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@email.com",
            Password = "password",
            Role = UserRole.Customer
        };

        DbContext.Users.AddRange(notAdmin);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _storeService.ListAllStores(notAdmin.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to view this information.");
    }

    [Fact]
    public async Task ListAllStores_ReturnListOfStores()
    {
        await ResetDatabaseAsync();
        
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@email.com",
            Password = "password",
            Role = UserRole.Admin
        };

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "password",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(admin, owner);

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Test Store",
            OwnerId = owner.Id
        };

        DbContext.Stores.AddRange(store);
        await DbContext.SaveChangesAsync();

        var listStores = await _storeService.ListAllStores(admin.Id);

        listStores.Should().NotBeEmpty();
        listStores.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListOwnStores_UserNotFound()
    {
        Func<Task> act = async () => await _storeService.ListOwnStores(Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task ListOwnStores_UserIsCustomer()
    {
        await ResetDatabaseAsync();    
        
        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@email.com",
            Password = "password",
            Role = UserRole.Customer
        };

        DbContext.Users.AddRange(customer);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _storeService.ListOwnStores(customer.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to view this information.");
        ;
    }

    [Fact]
    public async Task ListOwnStores_ReturnListOfOwnStores()
    {
        await ResetDatabaseAsync();
        
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "password",
            Role = UserRole.Seller
        };
        
        DbContext.Users.AddRange(owner);

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Test Store",
            OwnerId = owner.Id
                };
        DbContext.Stores.AddRange(store);
        await DbContext.SaveChangesAsync();
        
        var listStores = await _storeService.ListOwnStores(owner.Id);
        
        listStores.Should().NotBeEmpty();
        listStores.Should().HaveCount(1);
        listStores[0].Name.Should().Be("Test Store");
    }
    
    [Fact]
    public async Task GetStoreInfoDetails_UserNotFound()
    {
        await ResetDatabaseAsync();

        Func<Task> act = async () => await _storeService.GetStoreInfoDetails(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task GetStoreInfoDetails_UserIsCustomer()
    {
        await ResetDatabaseAsync();

        var customer = new User
        {
            Id = Guid.NewGuid(),
            Email = "customer@email.com",
            Password = "pwd",
            Role = UserRole.Customer
        };
        DbContext.Users.Add(customer);
        await DbContext.SaveChangesAsync();

        var storeId = Guid.NewGuid();

        Func<Task> act = async () => await _storeService.GetStoreInfoDetails(storeId, customer.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to view this information.");
    }

    [Fact]
    public async Task GetStoreInfoDetails_StoreNotFound()
    {
        await ResetDatabaseAsync();

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@email.com",
            Password = "pwd",
            Role = UserRole.Admin
        };
        DbContext.Users.Add(admin);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _storeService.GetStoreInfoDetails(Guid.NewGuid(), admin.Id);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Store not found.");
    }

    [Fact]
    public async Task GetStoreInfoDetails_SellerNotOwner()
    {
        await ResetDatabaseAsync();

        var seller = new User
        {
            Id = Guid.NewGuid(),
            Email = "seller@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };

        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(seller, owner);

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store",
            OwnerId = owner.Id
        };

        DbContext.Stores.Add(store);
        await DbContext.SaveChangesAsync();

        Func<Task> act = async () => await _storeService.GetStoreInfoDetails(store.Id, seller.Id);

        await act.Should().ThrowAsync<UnauthorizedUserException>()
            .WithMessage("This user is not authorized to view this information.");
    }

    [Fact]
    public async Task GetStoreInfoDetails_SellerOwnerReturnsDetails()
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
            Name = "Store",
            OwnerId = owner.Id
        };
        DbContext.Stores.Add(store);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product A",
            Description = "desc",
            Price = 10,
            StoreId = store.Id
        };
        DbContext.Products.Add(product);

        await DbContext.SaveChangesAsync();

        var details = await _storeService.GetStoreInfoDetails(store.Id, owner.Id);

        details.Name.Should().Be(store.Name);
        details.OwnerEmail.Should().Be(owner.Email);
        details.Products.Should().HaveCount(1);
        details.Products[0].Name.Should().Be("Product A");
    }

    [Fact]
    public async Task GetStoreInfoDetails_AdminReturnsDetails()
    {
        await ResetDatabaseAsync();

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@email.com",
            Password = "pwd",
            Role = UserRole.Admin
        };
        var owner = new User
        {
            Id = Guid.NewGuid(),
            Email = "owner@email.com",
            Password = "pwd",
            Role = UserRole.Seller
        };

        DbContext.Users.AddRange(admin, owner);

        var store = new Store
        {
            Id = Guid.NewGuid(),
            Name = "Store",
            OwnerId = owner.Id
        };
        DbContext.Stores.Add(store);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = "Product A",
            Description = "desc",
            Price = 20,
            StoreId = store.Id
        };
        DbContext.Products.Add(product);

        await DbContext.SaveChangesAsync();

        var details = await _storeService.GetStoreInfoDetails(store.Id, admin.Id);

        details.Name.Should().Be(store.Name);
        details.OwnerEmail.Should().Be(owner.Email);
        details.Products.Should().HaveCount(1);
        details.Products[0].Name.Should().Be("Product A");
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
}