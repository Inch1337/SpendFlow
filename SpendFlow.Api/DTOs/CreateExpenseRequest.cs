using System.ComponentModel.DataAnnotations;
using SpendFlow.Api.Entities;

namespace SpendFlow.Api.DTOs;

public class CreateExpenseRequest : IValidatableObject
{
    [Required]
    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "999999999.99",
        ErrorMessage = "Сумма должна быть от 0,01 до 999999999,99.",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal? Amount { get; set; }

    [Required]
    public DateOnly? Date { get; set; }

    [Required]
    [EnumDataType(typeof(ExpenseCategory))]
    public ExpenseCategory? Category { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount.HasValue && decimal.Round(Amount.Value, 2) != Amount.Value)
        {
            yield return new ValidationResult(
                "Сумма должна содержать не более двух знаков после запятой.",
                new[] { nameof(Amount) });
        }
    }
}
