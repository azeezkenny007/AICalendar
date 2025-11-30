using AICalendar.Application.Calendar.DTOs;
using AICalendar.Application.Calendar.Services;
using AICalendar.Application.Common.Models;
using AICalendar.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace AICalendar.Application.Calendar.Queries.GetCalendarPredictions;

public class GetCalendarPredictionsQueryHandler
    : IRequestHandler<GetCalendarPredictionsQuery, Result<CalendarPredictionResponseDto>>
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMapper _mapper;

    public GetCalendarPredictionsQueryHandler(
        ITransactionRepository transactionRepository,
        IMapper mapper)
    {
        _transactionRepository = transactionRepository;
        _mapper = mapper;
    }

    public async Task<Result<CalendarPredictionResponseDto>> Handle(
        GetCalendarPredictionsQuery request,
        CancellationToken cancellationToken)
    {
        // Step 1: Get all transactions for the user
        var transactions = await _transactionRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (!transactions.Any())
        {
            return Result.Failure<CalendarPredictionResponseDto>("No transactions found for user");
        }

        // Step 2: Extract transaction IDs
        var transactionIds = transactions.Select(t => t.Id.Value).ToList();

        // Step 3: Call Mock AI Service to get predictions (returns 10 predictions with transaction IDs)
        var aiPredictions = MockAIService.GenerateMockPredictions(request.UserId, transactionIds);

        // Step 4: Get the actual transaction details for those predicted transaction IDs
        var predictedTransactionIds = aiPredictions.Select(p => p.TransactionId).ToList();
        var predictedTransactions = transactions
            .Where(t => predictedTransactionIds.Contains(t.Id.Value))
            .ToList();

        // Step 5: Map to DTOs and enrich with AI prediction data
        var transactionDetails = new List<TransactionDetailDto>();

        foreach (var transaction in predictedTransactions)
        {
            var aiPrediction = aiPredictions.First(p => p.TransactionId == transaction.Id.Value);
            var status = PredictionStatusStore.GetStatus(transaction.Id.Value);

            var dto = _mapper.Map<TransactionDetailDto>(transaction);
            dto.PredictionStatus = status;
            dto.PredictedNextDate = aiPrediction.PredictedNextDate;
            dto.Confidence = aiPrediction.FinalConfidence;
            dto.AIExplanation = aiPrediction.Explanation;

            transactionDetails.Add(dto);
        }

        var response = new CalendarPredictionResponseDto
        {
            Transactions = transactionDetails.OrderByDescending(t => t.Confidence).ToList(),
            TotalPredictions = transactionDetails.Count,
            GeneratedAt = DateTime.UtcNow
        };

        return Result<CalendarPredictionResponseDto>.Success(response);
    }
}
