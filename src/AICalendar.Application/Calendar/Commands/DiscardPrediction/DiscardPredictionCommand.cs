using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.DiscardPrediction;

public record DiscardPredictionCommand(Guid TransactionId) : IRequest<Result<bool>>;
