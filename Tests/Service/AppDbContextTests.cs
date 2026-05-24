using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Domain.Models;
using Infra;

namespace Tests.Service;

public class AppDbContextTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ecommerce_testdb")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();
    private AppDbContext _dbContext = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .Options;
        
        _dbContext = new AppDbContext(options);
        
        await _dbContext.Database.MigrateAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task CheckDuplicityException()
    {
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
        
        _dbContext.Users.Add(testUser);
        _dbContext.Stores.Add(testStore);
        _dbContext.Products.Add(testProduct1);
        _dbContext.Products.Add(testProduct2);
        
        Func<Task> act = async () => await _dbContext.SaveChangesAsync();
        
        await act.Should().ThrowAsync<DbUpdateException>();
    }
    
    [Fact]
    public async Task CheckCascadeDeletion()
    {
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
        
        _dbContext.Users.Add(testUser);
        _dbContext.Stores.Add(testStore);
        _dbContext.Products.Add(testProduct);
        
        await _dbContext.SaveChangesAsync();
        
        _dbContext.ChangeTracker.Clear();
        
        var storeToDelete = await _dbContext.Stores.FindAsync(testStore.Id);
        
        _dbContext.Stores.Remove(storeToDelete!);
        await _dbContext.SaveChangesAsync();
        
        var productStillExists = await _dbContext.Products.AnyAsync(p => p.Id == testProduct.Id);
        
        productStillExists.Should().BeFalse();
    }
}