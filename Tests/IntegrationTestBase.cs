using System.Data.Common;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Respawn;
using Infra;
using Tests.Fixture;

namespace Tests;

[Collection("Database collection")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected readonly AppDbContext DbContext;
    private Respawner _respawner = default!;
    private DbConnection _connection = default!;

    protected IntegrationTestBase(DatabaseFixture databaseFixture)
    {
        DbContext = databaseFixture.GetDbContext();
    }

    public async Task InitializeAsync()
    {
        _connection = DbContext.Database.GetDbConnection();

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        _respawner ??= await Respawner.CreateAsync(_connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    protected async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_connection);
        DbContext.ChangeTracker.Clear();
    }
}