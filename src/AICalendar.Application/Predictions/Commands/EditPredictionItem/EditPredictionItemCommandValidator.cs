using FluentValidation;

namespace AICalendar.Application.Predictions.Commands.EditPredictionItem;

public class EditPredictionItemCommandValidator : AbstractValidator<EditPredictionItemCommand>
{
    public EditPredictionItemCommandValidator()
    {
        RuleFor(v => v.ItemId)
            .NotEmpty();

        // Fix for BUG-05A: Amount must be valid if provided
        RuleFor(v => v.Amount)
            .GreaterThan(0)
            .When(v => v.Amount.HasValue)
            .WithMessage("Amount must be greater than 0.");

        // Fix for BUG-05A: Merchant cannot be empty if provided
        RuleFor(v => v.Merchant)
            .NotEmpty()
            .When(v => v.Merchant != null)
            .WithMessage("Merchant cannot be empty.");

        // Fix for BUG-05A: Description cannot be empty if provided
        RuleFor(v => v.Description)
            .NotEmpty()
            .When(v => v.Description != null)
            .WithMessage("Description cannot be empty.");

        // Fix for BUG-05B: Unrealistic future dates (Max 5 years)
        RuleFor(v => v.DueDate)
            .LessThan(DateTime.UtcNow.AddYears(5))
            .When(v => v.DueDate.HasValue)
            .WithMessage("Due date cannot be more than 5 years in the future.");
    }
}
