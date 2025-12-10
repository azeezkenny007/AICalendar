using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictionItemsLimit;

public class GetUserPredictionItemsLimitQueryHandler
    : IRequestHandler<GetUserPredictionItemsLimitQuery, Result<List<PredictionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserRepository _userRepository;

    public GetUserPredictionItemsLimitQueryHandler(
        IApplicationDbContext context,
        IUserRepository userRepository)
    {
        _context = context;
        _userRepository = userRepository;
    }

    public async Task<Result<List<PredictionDto>>> Handle(
        GetUserPredictionItemsLimitQuery request,
        CancellationToken ct)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, ct);
        if (user == null)
        {
            return Result<List<PredictionDto>>.Failure($"User {request.UserId} not found.");
        }

        // Get predictions with items ordered by due date, limited to 10 total items
        var predictions = await _context.Predictions
            .Where(p => p.UserId == request.UserId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                Prediction = p,
                Items = p.Items.OrderBy(i => i.DueDate).ToList()
            })
            .ToListAsync(ct);

        // Flatten all items and take first 10
        var allItems = predictions
            .SelectMany(p => p.Items.Select(i => new
            {
                Item = i,
                PredictionId = p.Prediction.Id.Value,
                UserId = p.Prediction.UserId.Value,
                Status = p.Prediction.Status.ToString(),
                CreatedAt = p.Prediction.CreatedAt
            }))
            .OrderBy(x => x.Item.DueDate)
            .Take(request.Limit)
            .ToList();

        // Group back into predictions
        var result = allItems
            .GroupBy(x => x.PredictionId)
            .Select(g => new PredictionDto(
                g.Key,
                g.First().UserId,
                g.First().Status,
                g.First().CreatedAt,
                g.Select(x => new PredictionItemDto(
                    x.Item.Id.Value,
                    x.Item.Merchant,
                    x.Item.Amount,
                    x.Item.DueDate,
                    x.Item.Explanation,
                    x.Item.Confidence.Value,
                    x.Item.Pattern.ToString(),
                    x.Item.IsAccepted,
                    x.Item.IsEdited,
                    x.Item.Account,
                    x.Item.AccountName,
                    x.Item.Description
                )).ToList()
            ))
            .ToList();

        return Result<List<PredictionDto>>.Success(result);
    }
}
