using FluentValidation;

namespace AICalendar.Application.Calendar.Commands.DiscardPrediction;

public class DiscardPredictionCommandValidator : AbstractValidator<DiscardPredictionCommand>
{
    public DiscardPredictionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Transaction ID must be a valid GUID");
    }
}
