using System.Text.Json.Serialization;

namespace AICalendar.Domain.ValueObjects;

public record UserFeedbackId
{
    public Guid Value { get; init; }

    private UserFeedbackId() { }

    [JsonConstructor]
    public UserFeedbackId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserFeedbackId cannot be empty", nameof(value));

        Value = value;
    }

    public static UserFeedbackId Create() => new(Guid.NewGuid());
    public static UserFeedbackId Create(Guid value) => new(value);

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(UserFeedbackId id) => id.Value;
}