namespace AICalendar.Application.Calendar.Services;

/// <summary>
/// In-memory store for prediction statuses (Keep, Discard, Edit)
/// In production, this would be stored in the database
/// </summary>
public class PredictionStatusStore
{
    private static readonly Dictionary<Guid, PredictionStatus> _statuses = new();
    private static readonly object _lock = new();

    public static void SetStatus(Guid transactionId, string status)
    {
        lock (_lock)
        {
            if (_statuses.ContainsKey(transactionId))
            {
                _statuses[transactionId].Status = status;
                _statuses[transactionId].ModifiedAt = DateTime.UtcNow;
            }
            else
            {
                _statuses[transactionId] = new PredictionStatus
                {
                    TransactionId = transactionId,
                    Status = status,
                    ModifiedAt = DateTime.UtcNow
                };
            }
        }
    }

    public static string GetStatus(Guid transactionId)
    {
        lock (_lock)
        {
            return _statuses.ContainsKey(transactionId) ? _statuses[transactionId].Status : "Pending";
        }
    }

    public static void ClearAll()
    {
        lock (_lock)
        {
            _statuses.Clear();
        }
    }
}

public class PredictionStatus
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Kept, Discarded, Edited
    public DateTime ModifiedAt { get; set; }
}
