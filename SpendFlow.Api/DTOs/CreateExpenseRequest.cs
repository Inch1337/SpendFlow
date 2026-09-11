using System.ComponentModel.DataAnnotations;
using SpendFlow.Api.Entities;

namespace SpendFlow.Api.DTOs;

public class CreateExpenseRequest
{
    [Required]
    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [Range(typeof(decimal), "0", "79228162514264337593543950335",
        MinimumIsExclusive = true,
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    public decimal? Amount { get; set; }

    [Required]
    public DateOnly? Date { get; set; }

    [Required]
    [EnumDataType(typeof(ExpenseCategory))]
    public ExpenseCategory? Category { get; set; }
}
