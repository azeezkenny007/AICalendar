using System.Text.Json.Serialization;

namespace AICalendar.Domain.ValueObjects;

public record CalendarItemId
{
    public Guid Value { get; init; }

    private CalendarItemId() { }

    [JsonConstructor]
    public CalendarItemId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CalendarItemId cannot be empty", nameof(value));

        Value = value;
    }

    public static CalendarItemId Create() => new(Guid.NewGuid());
    public static CalendarItemId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CalendarItemId id) => id.Value;
}
