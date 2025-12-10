using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictionItemsLimit;

public class GetUserPredictionItemsLimitQueryHandler
    : IRequestHandler<GetUserPredictionItemsLimitQuery, Result<List<PredictionDto>>>
{
    private readonly IPredictionRepository _repository;
    private readonly IUserRepository _userRepository;

    public GetUserPredictionItemsLimitQueryHandler(
        IPredictionRepository repository,
        IUserRepository userRepository)
    {
        _repository = repository;
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

        // Get all predictions for the user from repository (fresh from DB)
        var predictions = await _repository.GetByUserIdAsync(request.UserId, ct);

        // Convert to DTOs and limit to first 10 items across all predictions
        var allPredictionsDto = predictions
            .Select(p => new PredictionDto(
                p.Id.Value,
                p.UserId.Value,
                p.Status.ToString(),
                p.CreatedAt,
                p.Items.Select(item => new PredictionItemDto(
                    item.Id.Value,
                    item.Merchant,
                    item.Amount,
                    item.DueDate,
                    item.Explanation,
                    item.Confidence.Value,
                    item.Pattern.ToString(),
                    item.IsAccepted,
                    item.IsEdited,
                    item.Account,
                    item.AccountName,
                    item.Description
                )).ToList()
            ))
            .ToList();

        // Flatten all items, take first 10, and group back into predictions
        var allItems = allPredictionsDto
            .SelectMany(p => p.Items.Select(item => new { Prediction = p, Item = item }))
            .Take(request.Limit)
            .ToList();

        // Group items back by prediction
        var result = allItems
            .GroupBy(x => x.Prediction.Id)
            .Select(g => new PredictionDto(
                g.Key,
                g.First().Prediction.UserId,
                g.First().Prediction.Status,
                g.First().Prediction.CreatedAt,
                g.Select(x => x.Item).ToList()
            ))
            .ToList();

        return Result<List<PredictionDto>>.Success(result);
    }
}
