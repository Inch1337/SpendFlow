using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpendFlow.Api.Controllers;

namespace SpendFlow.Tests;

// HTTP requests commit independently; keep them separate from the service tests' seed transactions.
[CollectionDefinition("Expense API", DisableParallelization = true)]
public class ExpensesApiCollection { }

[Collection("Expense API")]
[Trait("Category", "Integration")]
public sealed class ExpensesApiTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly PostgresFixture _database;
    private readonly WebApplicationFactory<ExpensesController> _application;
    private readonly HttpClient _client;
    private readonly string _description = $"API test {Guid.NewGuid()}";

    public ExpensesApiTests(PostgresFixture database)
    {
        _database = database;
        _application = new WebApplicationFactory<ExpensesController>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = PostgresFixture.ConnectionString
                })));
        _client = _application.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        try
        {
            // Also clean up when an assertion or response deserialization fails after POST.
            await using var context = _database.CreateContext();
            await context.Expenses.Where(expense => expense.Description == _description)
                .ExecuteDeleteAsync();
        }
        finally
        {
            _client.Dispose();
            await _application.DisposeAsync();
        }
    }

    [Fact]
    public async Task Get_UnknownId_ReturnsNotFound()
    {
        using var response = await _client.GetAsync("/api/expenses/-1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownId_ReturnsNotFound()
    {
        using var response = await _client.DeleteAsync("/api/expenses/-1");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ThenGet_ReturnsSavedExpense()
    {
        var created = await CreateExpenseAsync();
        using var response = await _client.GetAsync($"/api/expenses/{created.GetProperty("id").GetInt32()}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expense = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(created.GetProperty("id").GetInt32(), expense.GetProperty("id").GetInt32());
        Assert.Equal(_description, expense.GetProperty("description").GetString());
        Assert.Equal(12.30m, expense.GetProperty("amount").GetDecimal());
        Assert.Equal("2024-02-29", expense.GetProperty("date").GetString());
        Assert.Equal("Food", expense.GetProperty("category").GetString());
        // PostgreSQL timestamps retain microseconds; .NET timestamps can contain 100 ns ticks.
        var createdAt = created.GetProperty("createdAt").GetDateTimeOffset();
        var savedCreatedAt = expense.GetProperty("createdAt").GetDateTimeOffset();
        Assert.InRange((createdAt - savedCreatedAt).Ticks, 0L, 9L);
        Assert.Equal(TimeSpan.Zero, expense.GetProperty("createdAt").GetDateTimeOffset().Offset);
        Assert.Equal(JsonValueKind.Null, expense.GetProperty("updatedAt").ValueKind);
    }

    [Fact]
    public async Task Delete_CreatedExpense_SubsequentGetReturnsNotFound()
    {
        var created = await CreateExpenseAsync();
        var url = $"/api/expenses/{created.GetProperty("id").GetInt32()}";
        using var deleted = await _client.DeleteAsync(url);

        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        using var response = await _client.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<JsonElement> CreateExpenseAsync()
    {
        using var response = await _client.PostAsJsonAsync("/api/expenses", new
        {
            description = _description,
            amount = 12.30m,
            date = "2024-02-29",
            category = "Food"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var expense = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(expense.GetProperty("id").GetInt32() > 0);
        return expense;
    }
}
