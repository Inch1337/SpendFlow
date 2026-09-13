using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using SpendFlow.Api.Data;
using SpendFlow.Api.DTOs;
using SpendFlow.Api.Entities;
using SpendFlow.Api.Services;

namespace SpendFlow.Tests;

[Trait("Category", "Integration")]
public class ExpenseServiceIntegrationTests : IClassFixture<PostgresFixture>, IAsyncLifetime
{
    private readonly AppDbContext _context;
    private readonly ExpenseService _service;
    private IDbContextTransaction? _transaction;

    public ExpenseServiceIntegrationTests(PostgresFixture fixture)
    {
        _context = fixture.CreateContext();
        _service = new ExpenseService(_context, NullLogger<ExpenseService>.Instance);
    }

    public async Task InitializeAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
        _context.Expenses.AddRange(
            Expense(1, "Earlier", 100m, new DateOnly(2025, 12, 31), ExpenseCategory.Other),
            Expense(2, "Coffee", 10.25m, new DateOnly(2026, 1, 1), ExpenseCategory.Food),
            Expense(3, "COFFEE beans", 20.50m, new DateOnly(2026, 1, 15), ExpenseCategory.Food),
            Expense(4, "Rent", 40m, new DateOnly(2026, 1, 15), ExpenseCategory.Housing),
            Expense(5, "Bus", 30m, new DateOnly(2026, 1, 31), ExpenseCategory.Transport),
            Expense(6, "Future", 200m, new DateOnly(2026, 2, 1), ExpenseCategory.Health));
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
            }
        }
        finally
        {
            await _context.DisposeAsync();
        }
    }

    [Fact]
    public async Task Summary_GroupsAmountsAndIncludesAllCategories()
    {
        var result = await _service.GetSummaryAsync(new ExpenseSummaryQueryRequest
        {
            DateFrom = new DateOnly(2026, 1, 1),
            DateTo = new DateOnly(2026, 1, 31)
        });

        Assert.Equal(100.75m, result.TotalAmount);
        Assert.Equal(new[]
        {
            (ExpenseCategory.Food, 30.75m),
            (ExpenseCategory.Transport, 30m),
            (ExpenseCategory.Housing, 40m),
            (ExpenseCategory.Entertainment, 0m),
            (ExpenseCategory.Health, 0m),
            (ExpenseCategory.Other, 0m)
        }, result.ByCategory.Select(item => (item.Category, item.TotalAmount)).ToArray());
    }

    [Fact]
    public async Task Summary_WithoutDatesIncludesAllExpenses()
    {
        var result = await _service.GetSummaryAsync(new ExpenseSummaryQueryRequest());

        Assert.Equal(400.75m, result.TotalAmount);
    }

    [Fact]
    public async Task Summary_EmptyPeriodReturnsSixZeroCategories()
    {
        var result = await _service.GetSummaryAsync(new ExpenseSummaryQueryRequest
        {
            DateFrom = new DateOnly(2027, 1, 1)
        });

        Assert.Equal(0m, result.TotalAmount);
        Assert.Equal(6, result.ByCategory.Count);
        Assert.Equal(6, result.ByCategory.Select(item => item.Category).Distinct().Count());
        Assert.All(result.ByCategory, item => Assert.Equal(0m, item.TotalAmount));
    }

    public static TheoryData<DateOnly?, DateOnly?, int[], decimal> Periods => new()
    {
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), new[] { 5, 4, 3, 2 }, 100.75m },
        { new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15), new[] { 4, 3 }, 60.50m },
        { new DateOnly(2026, 1, 15), null, new[] { 6, 5, 4, 3 }, 290.50m },
        { null, new DateOnly(2026, 1, 15), new[] { 4, 3, 2, 1 }, 170.75m }
    };

    [Theory]
    [MemberData(nameof(Periods))]
    public async Task ListAndSummary_UseTheSameInclusivePeriod(
        DateOnly? dateFrom, DateOnly? dateTo, int[] expectedIds, decimal expectedTotal)
    {
        var list = await _service.GetAllAsync(new ExpenseQueryRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo
        });
        var summary = await _service.GetSummaryAsync(new ExpenseSummaryQueryRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo
        });

        Assert.Equal(expectedIds, list.Select(item => item.Id).ToArray());
        Assert.Equal(expectedTotal, summary.TotalAmount);
    }

    [Fact]
    public async Task List_WithoutFiltersSortsByDateThenIdDescending()
    {
        var result = await _service.GetAllAsync(new ExpenseQueryRequest());

        Assert.Equal(new[] { 6, 5, 4, 3, 2, 1 }, result.Select(item => item.Id).ToArray());
    }

    [Fact]
    public async Task List_CombinesDateCategoryAndSearchFilters()
    {
        _context.Expenses.Add(Expense(7, "Coffee taxi", 5m,
            new DateOnly(2026, 1, 15), ExpenseCategory.Transport));
        _context.Expenses.Add(Expense(8, "Tea", 5m,
            new DateOnly(2026, 1, 15), ExpenseCategory.Food));
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(new ExpenseQueryRequest
        {
            DateFrom = new DateOnly(2026, 1, 15),
            DateTo = new DateOnly(2026, 1, 31),
            Category = ExpenseCategory.Food,
            Search = "  coffee  "
        });

        Assert.Equal(3, Assert.Single(result).Id);
    }

    [Fact]
    public async Task List_SearchIsCaseInsensitiveAndTrimsWhitespace()
    {
        var result = await _service.GetAllAsync(new ExpenseQueryRequest { Search = "  cOfFeE  " });

        Assert.Equal(new[] { 3, 2 }, result.Select(item => item.Id).ToArray());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task List_IgnoresEmptySearch(string search)
    {
        var result = await _service.GetAllAsync(new ExpenseQueryRequest { Search = search });

        Assert.Equal(new[] { 6, 5, 4, 3, 2, 1 }, result.Select(item => item.Id).ToArray());
    }

    [Theory]
    [InlineData("%", "Discount 10%", "Discount 10X")]
    [InlineData("_", "item_one", "itemXone")]
    [InlineData("\\", "folder\\file", "folderfile")]
    public async Task List_SearchTreatsSpecialCharactersLiterally(
        string search, string matchingDescription, string otherDescription)
    {
        _context.Expenses.AddRange(
            Expense(7, matchingDescription, 1m, new DateOnly(2026, 1, 1), ExpenseCategory.Other),
            Expense(8, otherDescription, 1m, new DateOnly(2026, 1, 1), ExpenseCategory.Other));
        await _context.SaveChangesAsync();

        var result = await _service.GetAllAsync(new ExpenseQueryRequest { Search = search });

        Assert.Equal(7, Assert.Single(result).Id);
    }

    [Fact]
    public async Task List_NoMatchesReturnsEmptyList()
    {
        var result = await _service.GetAllAsync(new ExpenseQueryRequest { Search = "no matching expense" });

        Assert.Empty(result);
    }

    private static Expense Expense(int id, string description, decimal amount, DateOnly date,
        ExpenseCategory category) => new()
    {
        Id = id,
        Description = description,
        Amount = amount,
        Date = date,
        Category = category,
        CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    };
}
