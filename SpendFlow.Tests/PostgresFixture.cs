using Microsoft.EntityFrameworkCore;
using SpendFlow.Api.Data;

namespace SpendFlow.Tests;

public sealed class PostgresFixture : IAsyncLifetime
{
    // This connection belongs only to docker-compose.tests.yml, never to the API database.
    internal const string ConnectionString =
        "Host=localhost;Port=55432;Database=spendflow_tests;Username=spendflow_tests;Password=spendflow_tests;Timeout=5";

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new AppDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
