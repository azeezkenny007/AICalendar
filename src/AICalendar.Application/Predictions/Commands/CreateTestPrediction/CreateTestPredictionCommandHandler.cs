using AICalendar.Domain.Aggregates.PredictionAggregate;
using AICalendar.Domain.Common;
using AICalendar.Domain.Entities;
using AICalendar.Domain.Enums;
using AICalendar.Domain.Interfaces;
using AICalendar.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AICalendar.Application.Predictions.Commands.CreateTestPrediction;

public class CreateTestPredictionCommandHandler
    : IRequestHandler<CreateTestPredictionCommand, Result<TestPredictionResult>>
{
    private readonly IPredictionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationDbContext _context;

    public CreateTestPredictionCommandHandler(
        IPredictionRepository repository,
        IUnitOfWork unitOfWork,
        IApplicationDbContext context)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _context = context;
    }

    public async Task<Result<TestPredictionResult>> Handle(
        CreateTestPredictionCommand request,
        CancellationToken ct)
    {
        // Get a random user from the database using IApplicationDbContext
        var randomUser = await _context.Users
            .OrderBy(u => Guid.NewGuid())
            .FirstOrDefaultAsync(ct);

        if (randomUser == null)
        {
            return Result<TestPredictionResult>.Failure("No users found in database");
        }

        var userId = randomUser.Id;
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var cycle = PredictionCycle.Create(startDate, endDate);
        var prediction = Prediction.Create(userId, cycle);

        // Add 10 sample prediction items
        var sampleItems = new[]
        {
            new { Merchant = "Netflix", Amount = 15.99m, Days = 5, Description = "Monthly subscription detected", Confidence = 0.95, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Spotify", Amount = 9.99m, Days = 10, Description = "Monthly subscription detected", Confidence = 0.90, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Amazon Prime", Amount = 14.99m, Days = 3, Description = "Monthly subscription detected", Confidence = 0.92, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Apple iCloud", Amount = 2.99m, Days = 7, Description = "Monthly subscription detected", Confidence = 0.88, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Microsoft 365", Amount = 9.99m, Days = 15, Description = "Monthly subscription detected", Confidence = 0.93, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Gym Membership", Amount = 49.99m, Days = 1, Description = "Monthly recurring payment", Confidence = 0.85, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Internet Bill", Amount = 79.99m, Days = 20, Description = "Monthly utility bill", Confidence = 0.97, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Mobile Phone", Amount = 55.00m, Days = 12, Description = "Monthly phone bill", Confidence = 0.96, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Hulu", Amount = 7.99m, Days = 8, Description = "Monthly subscription detected", Confidence = 0.89, Pattern = PatternType.FixedDateRecurring },
            new { Merchant = "Disney+", Amount = 10.99m, Days = 18, Description = "Monthly subscription detected", Confidence = 0.91, Pattern = PatternType.FixedDateRecurring }
        };

        foreach (var item in sampleItems)
        {
            prediction.AddItem(PredictionItem.Create(
                Guid.NewGuid(),
                item.Merchant,
                item.Amount,
                DateTime.UtcNow.AddDays(item.Days),
                item.Description,
                ConfidenceScore.Create(item.Confidence, item.Confidence - 0.05, item.Confidence + 0.04),
                item.Pattern
            ));
        }

        prediction.MarkAsGenerated();

        await _repository.AddAsync(prediction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Result<TestPredictionResult>.Success(new TestPredictionResult(prediction.Id.Value, userId.Value));
    }
}
