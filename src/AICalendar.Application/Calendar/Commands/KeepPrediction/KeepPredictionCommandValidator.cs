using FluentValidation;

namespace AICalendar.Application.Calendar.Commands.KeepPrediction;

public class KeepPredictionCommandValidator : AbstractValidator<KeepPredictionCommand>
{
    public KeepPredictionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty()
            .WithMessage("Transaction ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("Transaction ID must be a valid GUID");
    }
}
