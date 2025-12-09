using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Users.Queries.GetUserById;

public record GetUserByIdQuery(UserId UserId) : IRequest<Result<UserDto>>;
