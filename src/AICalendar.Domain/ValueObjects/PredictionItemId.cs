namespace AICalendar.Domain.ValueObjects;

public record PredictionItemId
{
    public Guid Value { get; init; }

    private PredictionItemId() { }

    private PredictionItemId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PredictionItemId cannot be empty", nameof(value));

        Value = value;
    }

    public static PredictionItemId Create() => new(Guid.NewGuid());
    public static PredictionItemId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PredictionItemId id) => id.Value;
}
