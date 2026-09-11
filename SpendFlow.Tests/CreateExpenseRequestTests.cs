using System.ComponentModel.DataAnnotations;
using SpendFlow.Api.DTOs;
using SpendFlow.Api.Entities;

namespace SpendFlow.Tests;

public class CreateExpenseRequestTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void Description_RejectsMissingOrWhitespace(string? description)
    {
        var request = ValidRequest();
        request.Description = description;

        AssertInvalid(request, nameof(request.Description));
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(501, false)]
    public void Description_EnforcesMaximumLength(int length, bool expectedValid)
    {
        var request = ValidRequest();
        request.Description = new string('a', length);

        Assert.Equal(expectedValid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(nameof(CreateExpenseRequest.Amount))]
    [InlineData(nameof(CreateExpenseRequest.Date))]
    [InlineData(nameof(CreateExpenseRequest.Category))]
    public void RequiredFields_RejectMissingValues(string field)
    {
        var request = ValidRequest();
        switch (field)
        {
            case nameof(request.Amount): request.Amount = null; break;
            case nameof(request.Date): request.Date = null; break;
            case nameof(request.Category): request.Category = null; break;
        }

        AssertInvalid(request, field);
    }

    public static TheoryData<decimal, bool> AmountCases => new()
    {
        { -1m, false },
        { 0m, false },
        { 0.0000000000000000000000000001m, true },
        { 12.345m, true },
        { decimal.MaxValue, true }
    };

    [Theory]
    [MemberData(nameof(AmountCases))]
    public void Amount_AcceptsOnlyPositiveValues(decimal amount, bool expectedValid)
    {
        var request = ValidRequest();
        request.Amount = amount;

        Assert.Equal(expectedValid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void Category_RejectsUndefinedValues(int category)
    {
        var request = ValidRequest();
        request.Category = (ExpenseCategory)category;

        AssertInvalid(request, nameof(request.Category));
    }

    [Theory]
    [InlineData(ExpenseCategory.Food)]
    [InlineData(ExpenseCategory.Transport)]
    [InlineData(ExpenseCategory.Housing)]
    [InlineData(ExpenseCategory.Entertainment)]
    [InlineData(ExpenseCategory.Health)]
    [InlineData(ExpenseCategory.Other)]
    public void ValidRequest_AcceptsEachCategory(ExpenseCategory category)
    {
        var request = ValidRequest();
        request.Category = category;

        Assert.Empty(Validate(request));
    }

    [Fact]
    public void Date_AcceptsFutureCalendarDate()
    {
        var request = ValidRequest();
        request.Date = DateOnly.MaxValue;

        Assert.Empty(Validate(request));
    }

    private static CreateExpenseRequest ValidRequest() => new()
    {
        Description = "Coffee",
        Amount = 150.50m,
        Date = new DateOnly(2026, 1, 15),
        Category = ExpenseCategory.Food
    };

    private static List<ValidationResult> Validate(CreateExpenseRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results,
            validateAllProperties: true);
        return results;
    }

    private static void AssertInvalid(CreateExpenseRequest request, string field)
    {
        Assert.Contains(Validate(request), result => result.MemberNames.Contains(field));
    }
}
