using System.Text.Json.Serialization;

namespace AICalendar.Domain.ValueObjects;

public record CalendarId
{
    public Guid Value { get; init; }

    private CalendarId() { }

    [JsonConstructor]
    public CalendarId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("CalendarId cannot be empty", nameof(value));

        Value = value;
    }

    public static CalendarId Create() => new(Guid.NewGuid());
    public static CalendarId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(CalendarId id) => id.Value;
}
