using AICalendar.Domain.Common;
using MediatR;

namespace AICalendar.Application.Outbox.Commands.DeleteAllOutboxMessages;

/// <summary>
/// Command to delete all outbox messages from the system
/// </summary>
public record DeleteAllOutboxMessagesCommand : IRequest<Result<DeleteAllOutboxMessagesResponse>>;

/// <summary>
/// Response from delete all outbox messages command
/// </summary>
public record DeleteAllOutboxMessagesResponse(int DeletedRecordCount);
