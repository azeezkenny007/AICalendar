using FluentValidation;

namespace AICalendar.Application.Users.Commands.UnregisterDevice;

public class UnregisterDeviceCommandValidator : AbstractValidator<UnregisterDeviceCommand>
{
    public UnregisterDeviceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .NotEqual(Guid.Empty)
            .WithMessage("User ID must be a valid GUID");
    }
}
