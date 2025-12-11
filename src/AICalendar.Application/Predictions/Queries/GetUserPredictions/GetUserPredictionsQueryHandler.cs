using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictions;

public class GetUserPredictionsQueryHandler
    : IRequestHandler<GetUserPredictionsQuery, Result<List<PredictionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;

    public GetUserPredictionsQueryHandler(
        IApplicationDbContext context,
        IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }

    public async Task<Result<List<PredictionDto>>> Handle(
        GetUserPredictionsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            return Result<List<PredictionDto>>.Failure($"User {request.UserId} not found.");
        }

        var predictions = await _context.Predictions
            .Where(p => p.UserId == request.UserId)
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
