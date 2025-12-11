using System.Text.Json.Serialization;

namespace AICalendar.Domain.ValueObjects;

public record UserId
{
    public Guid Value { get; init; }

    // EF Core needs a parameterless constructor
    private UserId() { }

    [JsonConstructor]
    public UserId(Guid value)
    {
        Value = value;
    }

    public static UserId Create() => new(Guid.NewGuid());
    public static UserId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
