using FluentValidation;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictionsByMonth;

public class GetUserPredictionsByMonthQueryValidator : AbstractValidator<GetUserPredictionsByMonthQuery>
{
    public GetUserPredictionsByMonthQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithMessage("UserId is required.");

        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .WithMessage("Year must be between 2000 and 2100.");

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .WithMessage("Month must be between 1 and 12.");
    }
}
