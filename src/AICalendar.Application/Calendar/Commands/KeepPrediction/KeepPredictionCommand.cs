using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.KeepPrediction;

public record KeepPredictionCommand(Guid TransactionId) : IRequest<Result<bool>>;
