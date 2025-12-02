using AICalendar.Domain.Common;
using AICalendar.Domain.ValueObjects;
using MediatR;

namespace AICalendar.Application.Predictions.Commands.EditPredictionItem;

public record EditPredictionItemCommand(
    PredictionItemId ItemId,
    string Merchant,
    decimal Amount,
    DateTime DueDate
) : IRequest<Result>;
