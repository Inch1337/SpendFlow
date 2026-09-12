using System.ComponentModel.DataAnnotations;
using SpendFlow.Api.Entities;

namespace SpendFlow.Api.DTOs;

public class ExpenseQueryRequest : IValidatableObject
{
    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

    [EnumDataType(typeof(ExpenseCategory))]
    public ExpenseCategory? Category { get; set; }

    public string? Search { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DateFrom.HasValue && DateTo.HasValue && DateFrom.Value > DateTo.Value)
        {
            yield return new ValidationResult(
                "DateFrom must be on or before DateTo.",
                new[] { nameof(DateFrom), nameof(DateTo) });
        }
    }
}
