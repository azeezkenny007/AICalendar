using AICalendar.Application.Calendar.Services;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.KeepPrediction;

public class KeepPredictionCommandHandler : IRequestHandler<KeepPredictionCommand, Result<bool>>
{
    private readonly ITransactionRepository _transactionRepository;

    public KeepPredictionCommandHandler(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public async Task<Result<bool>> Handle(KeepPredictionCommand request, CancellationToken cancellationToken)
    {
        // Get the transaction from database
        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction == null)
        {
            return Result.Failure<bool>("Transaction not found");
        }

        // Mark as kept
        transaction.MarkAsKept();

        // Save to database
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);

        // Update the in-memory status to "Kept"
        PredictionStatusStore.SetStatus(request.TransactionId, "Kept");

        return Result<bool>.Success(true);
    }
}
