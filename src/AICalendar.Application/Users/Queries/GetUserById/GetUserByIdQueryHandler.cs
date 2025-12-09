using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetUserByIdQueryHandler> _logger;

    public GetUserByIdQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<GetUserByIdQueryHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching user with ID: {UserId}", request.UserId.Value);

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", request.UserId.Value);
                return Result<UserDto>.Failure($"User with ID {request.UserId.Value} not found");
            }

            var userDto = _mapper.Map<UserDto>(user);

            _logger.LogInformation("Successfully retrieved user: {Username}", user.Username);

            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user with ID: {UserId}", request.UserId.Value);
            return Result<UserDto>.Failure("Failed to retrieve user");
        }
    }
}
