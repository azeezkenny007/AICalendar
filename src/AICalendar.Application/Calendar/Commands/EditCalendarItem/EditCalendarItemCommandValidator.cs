using FluentValidation;

namespace AICalendar.Application.Calendar.Commands.EditCalendarItem;

public class EditCalendarItemCommandValidator : AbstractValidator<EditCalendarItemCommand>
{
    public EditCalendarItemCommandValidator()
    {
        RuleFor(x => x.ItemId)
            .NotNull()
            .WithMessage("Calendar item ID is required");

        RuleFor(x => x.ItemId.Value)
            .NotEmpty()
            .WithMessage("Calendar item ID value cannot be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("Calendar item ID must be a valid GUID");

        RuleFor(x => x.Merchant)
            .NotEmpty()
            .When(x => x.Merchant != null)
            .WithMessage("Merchant cannot be empty if provided")
            .MaximumLength(200)
            .When(x => x.Merchant != null)
            .WithMessage("Merchant cannot exceed 200 characters");

        When(x => x.Amount.HasValue, () =>
        {
            RuleFor(x => x.Amount!.Value)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than 0");
        });

        RuleFor(x => x.DueDate)
            .NotEmpty()
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date cannot be empty if provided");

        When(x => x.DueDate.HasValue, () =>
        {
            RuleFor(x => x.DueDate!.Value)
                .LessThanOrEqualTo(_ => DateTime.UtcNow.AddMonths(2))
                .WithMessage("Due date cannot be more than 2 months in the future");
        });

        RuleFor(x => x.Account)
            .NotEmpty()
            .When(x => x.Account != null)
            .WithMessage("Account cannot be empty if provided")
            .MaximumLength(100)
            .When(x => x.Account != null)
            .WithMessage("Account cannot exceed 100 characters");

        RuleFor(x => x.AccountName)
            .NotEmpty()
            .When(x => x.AccountName != null)
            .WithMessage("Account name cannot be empty if provided")
            .MaximumLength(200)
            .When(x => x.AccountName != null)
            .WithMessage("Account name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .When(x => x.Description != null)
            .WithMessage("Description cannot be empty if provided")
            .MaximumLength(500)
            .When(x => x.Description != null)
            .WithMessage("Description cannot exceed 500 characters");

        RuleFor(x => x)
            .Must(HaveAtLeastOneEditableField)
            .WithMessage("At least one field (Merchant, Amount, DueDate, Account, AccountName, or Description) must be provided for editing");
    }

    private bool HaveAtLeastOneEditableField(EditCalendarItemCommand command)
    {
        return command.Merchant != null ||
               command.Amount.HasValue ||
               command.DueDate.HasValue ||
               command.Account != null ||
               command.AccountName != null ||
               command.Description != null;
    }
}
