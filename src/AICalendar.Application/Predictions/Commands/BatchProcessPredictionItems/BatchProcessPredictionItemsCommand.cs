using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.BatchProcessPredictionItems;

public record BatchProcessPredictionItemsCommand(
    List<PredictionItemId> AcceptedItemIds,
    List<PredictionItemId> RejectedItemIds
) : IRequest<Result>;
