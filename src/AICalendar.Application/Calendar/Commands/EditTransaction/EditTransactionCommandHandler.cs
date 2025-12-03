using AICalendar.Application.Calendar.Services;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Entities;
using AICalendar.Domain.Interfaces;
using MediatR;

namespace AICalendar.Application.Calendar.Commands.EditTransaction;

public class EditTransactionCommandHandler : IRequestHandler<EditTransactionCommand, Result<bool>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public EditTransactionCommandHandler(ITransactionRepository transactionRepository,IUnitOfWork unitOfWork)
    {
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(EditTransactionCommand request, CancellationToken cancellationToken)
    {
        // Get the transaction from database
        var transaction = await _transactionRepository.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction == null)
        {
            return Result.Failure<bool>("Transaction not found");
        }

        // Update transaction fields if provided
        if (request.EditData.Amount.HasValue)
        {
            transaction.UpdateAmount(request.EditData.Amount.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.EditData.Description))
        {
            transaction.UpdateDescription(request.EditData.Description);
        }

        if (!string.IsNullOrWhiteSpace(request.EditData.TransactionType))
        {
            if (Enum.TryParse<TransactionType>(request.EditData.TransactionType, out var transactionType))
            {
                transaction.UpdateType(transactionType);
            }
        }

        if (request.EditData.ReceiverId != null)
        {
            transaction.UpdateReceiverId(request.EditData.ReceiverId);
        }

        if (request.EditData.MerchantId != null)
        {
            transaction.UpdateMerchantId(request.EditData.MerchantId);
        }

        // Save the updated transaction
        await _transactionRepository.UpdateAsync(transaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mark the prediction as "Edited"
        PredictionStatusStore.SetStatus(request.TransactionId, "Edited");

        return Result<bool>.Success(true);
    }
}
