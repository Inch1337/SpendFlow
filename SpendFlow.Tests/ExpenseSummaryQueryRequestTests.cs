using System.ComponentModel.DataAnnotations;
using SpendFlow.Api.DTOs;

namespace SpendFlow.Tests;

public class ExpenseSummaryQueryRequestTests
{
    public static TheoryData<DateOnly?, DateOnly?> ValidPeriods => new()
    {
        { null, null },
        { new DateOnly(2026, 1, 1), null },
        { null, new DateOnly(2026, 1, 31) },
        { new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15) },
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31) }
    };

    [Theory]
    [MemberData(nameof(ValidPeriods))]
    public void DateRange_AcceptsValidOrOpenPeriod(DateOnly? dateFrom, DateOnly? dateTo)
    {
        var request = new ExpenseSummaryQueryRequest
        {
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void DateRange_RejectsStartAfterEnd()
    {
        var request = new ExpenseSummaryQueryRequest
        {
            DateFrom = new DateOnly(2026, 2, 1),
            DateTo = new DateOnly(2026, 1, 31)
        };

        var error = Assert.Single(Validate(request));
        Assert.Contains(nameof(request.DateFrom), error.MemberNames);
        Assert.Contains(nameof(request.DateTo), error.MemberNames);
    }

    private static List<ValidationResult> Validate(ExpenseSummaryQueryRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results,
            validateAllProperties: true);
        return results;
    }
}
