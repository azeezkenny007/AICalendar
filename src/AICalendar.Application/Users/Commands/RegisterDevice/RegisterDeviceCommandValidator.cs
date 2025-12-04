using FluentValidation;

namespace AICalendar.Application.Users.Commands.RegisterDevice;

public class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");

        RuleFor(x => x.FcmToken)
            .NotEmpty()
            .WithMessage("FCM token is required")
            .MaximumLength(4096)
            .WithMessage("FCM token is too long")
            .Matches("^\\S+$")
            .WithMessage("FCM token cannot contain whitespace");
    }
}
