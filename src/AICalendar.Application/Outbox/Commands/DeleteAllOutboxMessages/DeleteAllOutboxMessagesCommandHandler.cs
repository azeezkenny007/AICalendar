using AICalendar.Domain.Common;
using AICalendar.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AICalendar.Application.Outbox.Commands.DeleteAllOutboxMessages;

/// <summary>
/// Handler for deleting all outbox messages
/// </summary>
public class DeleteAllOutboxMessagesCommandHandler : IRequestHandler<DeleteAllOutboxMessagesCommand, Result<DeleteAllOutboxMessagesResponse>>
{
    private readonly IOutboxMessageRepository _outboxMessageRepository;
    private readonly ILogger<DeleteAllOutboxMessagesCommandHandler> _logger;

    public DeleteAllOutboxMessagesCommandHandler(
        IOutboxMessageRepository outboxMessageRepository,
        ILogger<DeleteAllOutboxMessagesCommandHandler> logger)
    {
        _outboxMessageRepository = outboxMessageRepository;
        _logger = logger;
    }

    public async Task<Result<DeleteAllOutboxMessagesResponse>> Handle(
        DeleteAllOutboxMessagesCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogWarning("Starting deletion of all OutboxMessage records");

            // Get count of outbox messages before deletion
            var deletedCount = await _outboxMessageRepository.GetCountAsync(cancellationToken);

            if (deletedCount > 0)
            {
                // Delete all outbox messages
                await _outboxMessageRepository.DeleteAllAsync(cancellationToken);
                
                _logger.LogInformation($"Successfully deleted {deletedCount} OutboxMessage records");
            }
            else
            {
                _logger.LogInformation("No OutboxMessage records to delete");
            }

            return Result<DeleteAllOutboxMessagesResponse>.Success(
                new DeleteAllOutboxMessagesResponse(deletedCount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting OutboxMessage records");
            return Result<DeleteAllOutboxMessagesResponse>.Failure(ex.Message);
        }
    }
}
