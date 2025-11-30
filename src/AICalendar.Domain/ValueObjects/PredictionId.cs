namespace AICalendar.Domain.ValueObjects;

public record PredictionId
{
    public Guid Value { get; init; }

    private PredictionId() { }

    private PredictionId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PredictionId cannot be empty", nameof(value));

        Value = value;
    }

    public static PredictionId Create() => new(Guid.NewGuid());
    public static PredictionId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PredictionId id) => id.Value;
}
