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

        return Result<TestPredictionResult>.Success(new TestPredictionResult(prediction.Id.Value, userId.Value));
    }
}
