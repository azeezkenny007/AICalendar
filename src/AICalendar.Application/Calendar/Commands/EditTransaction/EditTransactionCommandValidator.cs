using AICalendar.Application.Calendar.DTOs;
using FluentValidation;

namespace AICalendar.Application.Calendar.Commands.EditTransaction;

public class EditTransactionCommandValidator : AbstractValidator<EditTransactionCommand>
{
    public EditTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Transaction ID must be a valid GUID");

        RuleFor(x => x.EditData)
            .NotNull()
            .WithMessage("Edit data is required");

        RuleFor(x => x.EditData.Amount)
            .GreaterThan(0)
            .When(x => x.EditData.Amount.HasValue)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.EditData.Description)
            .NotEmpty()
            .When(x => !string.IsNullOrWhiteSpace(x.EditData.Description))
            .WithMessage("Description cannot be empty if provided")
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.EditData.Description))
            .WithMessage("Description cannot exceed 500 characters");

        When(x => !string.IsNullOrWhiteSpace(x.EditData.TransactionType), () =>
        {
            RuleFor(x => x.EditData.TransactionType)
                .Must(BeValidTransactionType)
                .WithMessage("Invalid transaction type. Must be a valid TransactionType enum value");
        });

        RuleFor(x => x.EditData)
            .Must(HaveAtLeastOneField)
            .WithMessage("At least one field (Amount, Description, TransactionType, or TransactionDate) must be provided for editing");
    }

    private bool BeValidTransactionType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return true; // Let the NotEmpty rule handle this

        return Enum.TryParse<Domain.Entities.TransactionType>(type, true, out _);
    }

    private bool HaveAtLeastOneField(EditTransactionDto editData)
    {
        return editData.Amount.HasValue ||
               !string.IsNullOrWhiteSpace(editData.Description) ||
               !string.IsNullOrWhiteSpace(editData.TransactionType) ||
               editData.TransactionDate.HasValue;
    }
}
