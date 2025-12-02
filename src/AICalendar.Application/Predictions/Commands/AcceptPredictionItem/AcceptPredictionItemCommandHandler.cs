using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.Commands.AcceptPredictionItem;

public class AcceptPredictionItemCommandHandler
    : IRequestHandler<AcceptPredictionItemCommand, Result>
{
    private readonly IPredictionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptPredictionItemCommandHandler> _logger;

    public AcceptPredictionItemCommandHandler(
        IPredictionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AcceptPredictionItemCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        AcceptPredictionItemCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Accepting prediction item {ItemId}",
            request.ItemId
        );

        var prediction = await _repository.GetByItemIdAsync(request.ItemId, ct);

        if (prediction == null)
        {
            return Result.Failure("Prediction not found for item");
        }

        var result = prediction.AcceptItem(request.ItemId);

        if (!result.IsSuccess)
        {
            return result;
        }

        await _repository.UpdateAsync(prediction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Successfully accepted prediction item {ItemId}",
            request.ItemId
        );

        return Result.Success();
    }
}
