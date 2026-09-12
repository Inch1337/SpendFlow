using Microsoft.EntityFrameworkCore;
using SpendFlow.Api.Data;
using SpendFlow.Api.DTOs;
using SpendFlow.Api.Entities;

namespace SpendFlow.Api.Services;

public class ExpenseService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(AppDbContext dbContext, ILogger<ExpenseService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ExpenseResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var expense = await _dbContext.Expenses
            .AsNoTracking()
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);

        if (expense is null)
        {
            return null;
        }

        return new ExpenseResponse
        {
            Id = expense.Id,
            Description = expense.Description,
            Amount = expense.Amount,
            Date = expense.Date,
            Category = expense.Category,
            CreatedAt = expense.CreatedAt,
            UpdatedAt = expense.UpdatedAt
        };
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var expense = await _dbContext.Expenses
            .FirstOrDefaultAsync(expense => expense.Id == id, cancellationToken);

        if (expense is null)
        {
            return false;
        }

        _dbContext.Expenses.Remove(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted expense {ExpenseId}", expense.Id);
        return true;
    }

    // The caller must validate the request using DataAnnotations before calling this method.
    public async Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var expense = new Expense
        {
            Description = request.Description!.Trim(),
            Amount = request.Amount!.Value,
            Date = request.Date!.Value,
            Category = request.Category!.Value,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null
        };

        _dbContext.Expenses.Add(expense);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created expense {ExpenseId}", expense.Id);

        return new ExpenseResponse
        {
            Id = expense.Id,
            Description = expense.Description,
            Amount = expense.Amount,
            Date = expense.Date,
            Category = expense.Category,
            CreatedAt = expense.CreatedAt,
            UpdatedAt = expense.UpdatedAt
        };
    }
}
