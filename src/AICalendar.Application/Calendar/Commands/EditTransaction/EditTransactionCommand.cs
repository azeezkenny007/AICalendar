using AICalendar.Application.Calendar.DTOs;
using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.EditTransaction;

public record EditTransactionCommand(Guid TransactionId, EditTransactionDto EditData) : IRequest<Result<bool>>;
