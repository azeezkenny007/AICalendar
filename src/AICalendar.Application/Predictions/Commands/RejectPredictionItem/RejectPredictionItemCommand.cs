using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.RejectPredictionItem;

public record RejectPredictionItemCommand(
    PredictionItemId ItemId
) : IRequest<Result>;
