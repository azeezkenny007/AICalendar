using AICalendar.Application.Calendar.DTOs;
using AICalendar.Application.Common.Models;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetKeptTransactions;

public record GetKeptTransactionsQuery(Guid UserId) : IRequest<Result<List<TransactionDetailDto>>>;
