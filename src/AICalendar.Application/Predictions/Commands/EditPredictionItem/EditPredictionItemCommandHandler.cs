using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Predictions.Commands.EditPredictionItem;

public class EditPredictionItemCommandHandler
    : IRequestHandler<EditPredictionItemCommand, Result>
{
    private readonly IPredictionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EditPredictionItemCommandHandler> _logger;

    public EditPredictionItemCommandHandler(
        IPredictionRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<EditPredictionItemCommandHandler> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        EditPredictionItemCommand request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Editing prediction item {ItemId}",
            request.ItemId
        );

        var prediction = await _repository.GetByItemIdAsync(request.ItemId, ct);

        if (prediction == null)
        {
            return Result.Failure("Prediction not found for item");
        }

        var result = prediction.EditItem(
            request.ItemId,
            request.Merchant,
            request.Amount,
            request.DueDate
        );

        if (!result.IsSuccess)
        {
            return result;
        }

        await _repository.UpdateAsync(prediction, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Successfully edited prediction item {ItemId}",
            request.ItemId
        );

        return Result.Success();
    }
}
