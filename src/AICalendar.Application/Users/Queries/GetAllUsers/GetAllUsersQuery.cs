using AICalendar.Domain.Common;
using MediatR;

namespace AICalendar.Application.Users.Queries.GetAllUsers;

public record GetAllUsersQuery : IRequest<Result<List<UserDto>>>;
