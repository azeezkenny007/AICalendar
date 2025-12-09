using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictionsByMonth;

public class GetUserPredictionsByMonthQueryHandler
    : IRequestHandler<GetUserPredictionsByMonthQuery, Result<List<PredictionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;

    public GetUserPredictionsByMonthQueryHandler(
        IApplicationDbContext context,
        IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }

    public async Task<Result<List<PredictionDto>>> Handle(
        GetUserPredictionsByMonthQuery request,
        CancellationToken cancellationToken)
    {
        // Validate month and year
        if (request.Month < 1 || request.Month > 12)
        {
            return Result<List<PredictionDto>>.Failure("Month must be between 1 and 12.");
        }

        if (request.Year < 2000 || request.Year > 2100)
        {
            return Result<List<PredictionDto>>.Failure("Year must be between 2000 and 2100.");
        }

        // Verify user exists
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<List<PredictionDto>>.Failure($"User {request.UserId} not found.");
        }

        // Calculate the start and end dates for the requested month
        var startDate = new DateTime(request.Year, request.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Query predictions where the cycle overlaps with the requested month
        var predictions = await _context.Predictions
            .Where(p => p.UserId == request.UserId
                && p.Cycle.StartDate.Year == request.Year
                && p.Cycle.StartDate.Month == request.Month)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PredictionDto(
                p.Id.Value,
                p.UserId.Value,
                p.Status.ToString(),
                p.CreatedAt,
                p.Items.Select(i => new PredictionItemDto(
                    i.Id.Value,
                    i.Merchant,
                    i.Amount,
                    i.DueDate,
                    i.Explanation,
                    i.Confidence.Value,
                    i.Pattern.ToString(),
                    i.IsAccepted,
                    i.IsEdited,
                    i.Account,
                    i.AccountName,
                    i.Description
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<List<PredictionDto>>.Success(predictions);
    }
}
