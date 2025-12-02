using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.Commands.RejectPredictionItem;

public class RejectPredictionItemCommandHandler
    : IRequestHandler<RejectPredictionItemCommand, Result>
{
    private readonly IPredictionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RejectPredictionItemCommandHandler> _logger;

    public RejectPredictionItemCommandHandler(
        IPredictionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RejectPredictionItemCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        RejectPredictionItemCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Rejecting prediction item {ItemId}",
            request.ItemId
        );

        var prediction = await _repository.GetByItemIdAsync(request.ItemId, ct);

        if (prediction == null)
        {
            return Result.Failure("Prediction not found for item");
        }

        var result = prediction.RejectItem(request.ItemId);

        if (!result.IsSuccess)
        {
            return result;
        }

        await _repository.UpdateAsync(prediction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Successfully rejected prediction item {ItemId}",
            request.ItemId
        );

        return Result.Success();
    }
}
