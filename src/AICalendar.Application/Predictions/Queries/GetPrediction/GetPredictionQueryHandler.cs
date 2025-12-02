using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Predictions.Queries.GetPrediction;

public class GetPredictionQueryHandler : IRequestHandler<GetPredictionQuery, Result<PredictionDto>>
{
    private readonly IPredictionRepository _repository;

    public GetPredictionQueryHandler(IPredictionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PredictionDto>> Handle(GetPredictionQuery request, CancellationToken ct)
    {
        var prediction = await _repository.GetByIdAsync(request.Id, ct);

        if (prediction == null)
        {
            return Result<PredictionDto>.Failure("Prediction not found");
        }

        var dto = new PredictionDto(
            prediction.Id.Value,
            prediction.UserId.Value,
            prediction.Status.ToString(),
            prediction.CreatedAt,
            prediction.Items.Select(i => new PredictionItemDto(
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
        );

        return Result<PredictionDto>.Success(dto);
    }
}
