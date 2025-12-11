using FluentValidation;

namespace AICalendar.Application.Calendar.Queries.GetCalendarPredictions;

public class GetCalendarPredictionsQueryValidator : AbstractValidator<GetCalendarPredictionsQuery>
{
    public GetCalendarPredictionsQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");
    }
}
