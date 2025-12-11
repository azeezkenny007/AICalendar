using AICalendar.Application.Calendar.DTOs;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetKeptTransactions;

public class GetKeptTransactionsQueryHandler : IRequestHandler<GetKeptTransactionsQuery, Result<List<TransactionDetailDto>>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMapper _mapper;

    public GetKeptTransactionsQueryHandler(ITransactionRepository transactionRepository, IMapper mapper)
    {
        _transactionRepository = transactionRepository;
        _mapper = mapper;
    }

    public async Task<Result<List<TransactionDetailDto>>> Handle(GetKeptTransactionsQuery request, CancellationToken cancellationToken)
    {
        // Get all transactions for the user
        var transactions = await _transactionRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (transactions == null || !transactions.Any())
        {
            return Result.Failure<List<TransactionDetailDto>>("No transactions found for this user");
        }

        // Filter only kept transactions
        var keptTransactions = transactions.Where(t => t.IsKept).ToList();

        if (!keptTransactions.Any())
        {
            return Result<List<TransactionDetailDto>>.Success(new List<TransactionDetailDto>());
        }

        // Map to DTOs
        var transactionDtos = keptTransactions.Select(t => _mapper.Map<TransactionDetailDto>(t)).ToList();

        return Result<List<TransactionDetailDto>>.Success(transactionDtos);
    }
}
