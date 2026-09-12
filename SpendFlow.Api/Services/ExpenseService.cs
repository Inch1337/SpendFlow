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

    public async Task<List<ExpenseResponse>> GetAllAsync(
        ExpenseQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Expenses.AsNoTracking();

        if (request.DateFrom.HasValue)
        {
            query = query.Where(expense => expense.Date >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(expense => expense.Date <= request.DateTo.Value);
        }

        if (request.Category.HasValue)
        {
            query = query.Where(expense => expense.Category == request.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // Treat LIKE wildcard and escape characters as literal search text.
            var search = request.Search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
            var pattern = $"%{search}%";
            query = query.Where(expense => EF.Functions.ILike(expense.Description, pattern, "\\"));
        }

        return await query
            .OrderByDescending(expense => expense.Date)
            .ThenByDescending(expense => expense.Id)
            .Select(expense => new ExpenseResponse
            {
                Id = expense.Id,
                Description = expense.Description,
                Amount = expense.Amount,
                Date = expense.Date,
                Category = expense.Category,
                CreatedAt = expense.CreatedAt,
                UpdatedAt = expense.UpdatedAt
            })
            .ToListAsync(cancellationToken);
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
