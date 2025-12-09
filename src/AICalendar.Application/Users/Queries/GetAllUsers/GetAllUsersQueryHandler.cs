using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Users.Queries.GetAllUsers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, Result<List<UserDto>>>
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllUsersQueryHandler> _logger;

    public GetAllUsersQueryHandler(
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<GetAllUsersQueryHandler> logger)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<List<UserDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching all users");

            var users = await _userRepository.GetAllAsync(cancellationToken);
            var userDtos = _mapper.Map<List<UserDto>>(users);

            _logger.LogInformation("Successfully retrieved {Count} users", userDtos.Count);

            return Result<List<UserDto>>.Success(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all users");
            return Result<List<UserDto>>.Failure("Failed to retrieve users");
        }
    }
}
