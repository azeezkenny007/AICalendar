using FluentValidation;

namespace AICalendar.Application.Calendar.Commands.MarkItemAsPaid;

public class MarkItemAsPaidCommandValidator : AbstractValidator<MarkItemAsPaidCommand>
{
    public MarkItemAsPaidCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotNull()
            .WithMessage("Calendar item ID is required");

        RuleFor(x => x.ItemId.Value)
            .NotEmpty()
            .WithMessage("Calendar item ID value cannot be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("Calendar item ID must be a valid GUID");

        RuleFor(x => x.PaidDate)
            .NotEmpty()
            .WithMessage("Paid date is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Paid date cannot be in the future");
    }
}
