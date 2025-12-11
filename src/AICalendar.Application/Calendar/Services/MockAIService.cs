using AICalendar.Application.Calendar.DTOs;

namespace AICalendar.Application.Calendar.Services;

/// <summary>
/// Mock AI Service that generates predictions based on transaction IDs
/// In production, this would call the actual AI/ML engine via gRPC
/// </summary>
public class MockAIService
{
    /// <summary>
    /// Generates 10 mock AI predictions for a given user
    /// These predictions reference actual transaction IDs that will be fetched from the database
    /// </summary>
    public static List<AIPredictionDto> GenerateMockPredictions(Guid userId, List<Guid> transactionIds)
    {
        var predictions = new List<AIPredictionDto>();
        var baseDate = DateTime.UtcNow;

        // Take first 10 transaction IDs to create predictions
        var selectedTransactionIds = transactionIds.Take(10).ToList();

        var mockScenarios = new[]
        {
            new { Confidence = 0.92, Days = 15, Explanation = "Monthly electricity bill - PHCN/EKEDC payment detected on 15th of each month for last 3 months" },
            new { Confidence = 0.88, Days = 5, Explanation = "Recurring internet bill - Spectranet/Airtel Fiber payment occurs monthly" },
            new { Confidence = 0.95, Days = 1, Explanation = "Monthly rent payment - consistent pattern detected on 1st of each month" },
            new { Confidence = 0.85, Days = 10, Explanation = "DSTV/GOtv subscription - renews monthly on the 10th" },
            new { Confidence = 0.78, Days = 25, Explanation = "Monthly savings transfer - automated transfer to savings account detected" },
            new { Confidence = 0.72, Days = 7, Explanation = "Bi-weekly fuel purchase pattern - occurs every 14 days" },
            new { Confidence = 0.68, Days = 6, Explanation = "Weekly grocery shopping at Shoprite/Spar - consistent pattern on weekends" },
            new { Confidence = 0.82, Days = 20, Explanation = "Monthly gym membership fee - payment detected on 20th each month" },
            new { Confidence = 0.75, Days = 3, Explanation = "Bi-weekly data bundle purchase - MTN/Airtel/Glo pattern detected" },
            new { Confidence = 0.80, Days = 28, Explanation = "Monthly family support transfer - consistent pattern on 28th of each month" }
        };

        for (int i = 0; i < Math.Min(selectedTransactionIds.Count, 10); i++)
        {
            var scenario = mockScenarios[i];
            var ruleConf = scenario.Confidence + new Random().NextDouble() * 0.03;
            var mlConf = scenario.Confidence - new Random().NextDouble() * 0.03;

            predictions.Add(new AIPredictionDto
            {
                TransactionId = selectedTransactionIds[i],
                FinalConfidence = Math.Round(scenario.Confidence, 2),
                IsRecurring = true,
                PredictedNextDate = baseDate.AddDays(scenario.Days),
                Explanation = scenario.Explanation,
                RuleConfidence = Math.Round(ruleConf, 2),
                MLConfidence = Math.Round(mlConf, 2)
            });
        }

        return predictions;
    }
}
