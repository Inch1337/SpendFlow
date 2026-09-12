namespace SpendFlow.Api.DTOs;

public class ExpenseSummaryResponse
{
    public decimal TotalAmount { get; set; }

    public List<ExpenseCategorySummaryResponse> ByCategory { get; set; } = new();
}
