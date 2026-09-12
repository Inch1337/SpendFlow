using System.ComponentModel.DataAnnotations;
using SpendFlow.Api.DTOs;
using SpendFlow.Api.Entities;

namespace SpendFlow.Tests;

public class ExpenseQueryRequestTests
{
    [Fact]
    public void EmptyQuery_IsValid()
    {
        Assert.Empty(Validate(new ExpenseQueryRequest()));
    }

    public static TheoryData<DateOnly?, DateOnly?> ValidPeriods => new()
    {
        { new DateOnly(2026, 1, 1), null },
        { null, new DateOnly(2026, 1, 31) },
        { new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 15) },
        { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31) }
    };

    [Theory]
    [MemberData(nameof(ValidPeriods))]
    public void DateRange_AcceptsValidOrOpenPeriod(DateOnly? dateFrom, DateOnly? dateTo)
    {
        var request = new ExpenseQueryRequest { DateFrom = dateFrom, DateTo = dateTo };

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void DateRange_RejectsStartAfterEnd()
    {
        var request = new ExpenseQueryRequest
        {
            DateFrom = new DateOnly(2026, 2, 1),
            DateTo = new DateOnly(2026, 1, 31)
        };

        var error = Assert.Single(Validate(request));
        Assert.Contains(nameof(request.DateFrom), error.MemberNames);
        Assert.Contains(nameof(request.DateTo), error.MemberNames);
    }

    [Theory]
    [InlineData(ExpenseCategory.Food)]
    [InlineData(ExpenseCategory.Transport)]
    [InlineData(ExpenseCategory.Housing)]
    [InlineData(ExpenseCategory.Entertainment)]
    [InlineData(ExpenseCategory.Health)]
    [InlineData(ExpenseCategory.Other)]
    public void Category_AcceptsDefinedValues(ExpenseCategory category)
    {
        Assert.Empty(Validate(new ExpenseQueryRequest { Category = category }));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void Category_RejectsUndefinedValues(int category)
    {
        var request = new ExpenseQueryRequest { Category = (ExpenseCategory)category };

        Assert.Contains(Validate(request), error => error.MemberNames.Contains(nameof(request.Category)));
    }

    private static List<ValidationResult> Validate(ExpenseQueryRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results,
            validateAllProperties: true);
        return results;
    }
}
