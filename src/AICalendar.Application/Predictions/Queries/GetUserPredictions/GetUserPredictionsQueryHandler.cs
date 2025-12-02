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

    public GetUserPredictionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<PredictionDto>>> Handle(
        GetUserPredictionsQuery request,
        CancellationToken cancellationToken)
    {
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
                    i.IsEdited
                )).ToList()
            ))
            .ToListAsync(cancellationToken);

        return Result<List<PredictionDto>>.Success(predictions);
    }
}
