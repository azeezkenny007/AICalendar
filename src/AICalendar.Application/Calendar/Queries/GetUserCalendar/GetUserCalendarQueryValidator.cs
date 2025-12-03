using FluentValidation;

namespace AICalendar.Application.Calendar.Queries.GetUserCalendar;

public class GetUserCalendarQueryValidator : AbstractValidator<GetUserCalendarQuery>
{
    public GetUserCalendarQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotNull()
            .WithMessage("User ID is required");

        RuleFor(x => x.UserId.Value)
            .NotEmpty()
            .WithMessage("User ID value cannot be empty")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");
    }
}
