using Microsoft.EntityFrameworkCore;
using Infra;
using Tests.Fixture;

namespace Tests;

[Collection("Database collection")]
public abstract class IntegrationTestBase
{
    protected readonly AppDbContext DbContext;

    protected IntegrationTestBase(DatabaseFixture databaseFixture)
    {
        DbContext = databaseFixture.GetDbContext();
    }

    protected async Task ResetDatabaseAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'replica'");

        try
        {
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"CartItems\" CASCADE");
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Products\" CASCADE");
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Stores\" CASCADE");
            await DbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Users\" CASCADE");
        }
        finally
        {
            await DbContext.Database.ExecuteSqlRawAsync("SET session_replication_role = 'origin'");
        }

        DbContext.ChangeTracker.Clear();
    }
}