using AICalendar.Application.Predictions.Queries.GetPrediction;
using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Predictions.Queries.GetUserPredictions;

public class GetUserPredictionsQueryHandler
    : IRequestHandler<GetUserPredictionsQuery, Result<List<PredictionDto>>>
{
    private readonly IPredictionRepository _repository;
    private readonly IUserRepository _userRepository;

    public GetUserPredictionsQueryHandler(
        IPredictionRepository repository,
        IUserRepository userRepository)
    {
        _repository = repository;
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

        // Get predictions from repository (fresh from database)
        var predictions = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        // Convert to DTOs
        var result = predictions
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
            .ToList();

        return Result<List<PredictionDto>>.Success(result);
    }
}
