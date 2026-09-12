using System.ComponentModel.DataAnnotations;

namespace SpendFlow.Api.DTOs;

public class ExpenseSummaryQueryRequest : IValidatableObject
{
    public DateOnly? DateFrom { get; set; }

    public DateOnly? DateTo { get; set; }

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
