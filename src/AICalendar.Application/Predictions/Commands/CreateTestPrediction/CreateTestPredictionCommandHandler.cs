using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.Entities;
using AICalendar.Domain.Enums;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.CreateTestPrediction;

public class CreateTestPredictionCommandHandler
    : IRequestHandler<CreateTestPredictionCommand, Result<PredictionId>>
{
    private readonly IPredictionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTestPredictionCommandHandler(
        IPredictionRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PredictionId>> Handle(
        CreateTestPredictionCommand request,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var cycle = PredictionCycle.Create(startDate, endDate);
        var prediction = Prediction.Create(request.UserId, cycle);

        // Add some dummy items
        prediction.AddItem(PredictionItem.Create(
            Guid.NewGuid(),
            "Netflix",
            15.99m,
            DateTime.UtcNow.AddDays(5),
            "Monthly subscription detected",
            ConfidenceScore.Create(0.95, 0.9, 0.99),
            PatternType.FixedDateRecurring
        ));

        prediction.AddItem(PredictionItem.Create(
            Guid.NewGuid(),
            "Spotify",
            9.99m,
            DateTime.UtcNow.AddDays(10),
            "Monthly subscription detected",
            ConfidenceScore.Create(0.9, 0.85, 0.95),
            PatternType.FixedDateRecurring
        ));

        prediction.MarkAsGenerated();

        await _repository.AddAsync(prediction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<PredictionId>.Success(prediction.Id);
    }
}
