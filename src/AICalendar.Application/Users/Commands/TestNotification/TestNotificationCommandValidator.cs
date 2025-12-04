using FluentValidation;

namespace AICalendar.Application.Users.Commands.TestNotification;

public class TestNotificationCommandValidator : AbstractValidator<TestNotificationCommand>
{
    public TestNotificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");
    }
}
