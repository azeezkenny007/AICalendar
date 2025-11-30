using AICalendar.Application.Calendar.Services;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.DiscardPrediction;

public class DiscardPredictionCommandHandler : IRequestHandler<DiscardPredictionCommand, Result<bool>>
{
    private readonly ITransactionRepository _transactionRepository;

    public DiscardPredictionCommandHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<bool>> Handle(DiscardPredictionCommand request, CancellationToken cancellationToken)
    {
        // Get the transaction from database
        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction == null)
        {
            return Result.Failure<bool>("Transaction not found");
        }

        // Mark as discarded
        transaction.MarkAsDiscarded();

        // Save to database
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        // Update the in-memory status to "Discarded"
        PredictionStatusStore.SetStatus(request.TransactionId, "Discarded");

        return Result<bool>.Success(true);
    }
}
