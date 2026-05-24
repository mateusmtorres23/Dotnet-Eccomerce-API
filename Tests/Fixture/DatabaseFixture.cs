using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Infra;

namespace Tests.Fixture;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("ecommerce_testdb")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    private AppDbContext? _dbContext;

    public AppDbContext GetDbContext()
    {
        if (_dbContext == null)
            throw new InvalidOperationException("DatabaseFixture has not been initialized. Call InitializeAsync first.");
        
        return _dbContext;
    }

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
        if (_dbContext != null)
        {
            await _dbContext.DisposeAsync();
        }

        await _dbContainer.DisposeAsync();
    }

    public string GetConnectionString() => _dbContainer.GetConnectionString();
}