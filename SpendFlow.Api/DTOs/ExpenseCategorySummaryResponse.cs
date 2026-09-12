using SpendFlow.Api.Entities;

namespace SpendFlow.Api.DTOs;

public class ExpenseCategorySummaryResponse
{
    public ExpenseCategory Category { get; set; }

    public decimal TotalAmount { get; set; }
}
