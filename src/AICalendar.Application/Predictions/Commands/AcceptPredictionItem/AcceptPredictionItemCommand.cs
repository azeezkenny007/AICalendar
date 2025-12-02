using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.AcceptPredictionItem;

public record AcceptPredictionItemCommand(
    PredictionItemId ItemId
) : IRequest<Result>;
