using SpendFlow.Api.Entities;

namespace SpendFlow.Api.DTOs;

public class ExpenseResponse
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateOnly Date { get; set; }

    public ExpenseCategory Category { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
