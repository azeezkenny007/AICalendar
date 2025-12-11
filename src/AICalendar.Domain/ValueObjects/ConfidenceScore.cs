using AICalendar.Domain.Common;

namespace AICalendar.Domain.ValueObjects;

public class ConfidenceScore : ValueObject
{
    public double Value { get; }
    public double? RuleConfidence { get; }
    public double? MlConfidence { get; }

    public string Level => Value switch
    {
        >= 0.85 => "High",
        >= 0.60 => "Medium",
        _ => "Low"
    };

    private ConfidenceScore(double value, double? ruleConfidence, double? mlConfidence)
    {
        if (value < 0.0 || value > 1.0)
        {
            throw new ArgumentException("Confidence score must be between 0.0 and 1.0", nameof(value));
        }

        Value = value;
        RuleConfidence = ruleConfidence;
        MlConfidence = mlConfidence;
    }

    public static ConfidenceScore Create(double value, double? ruleConfidence = null, double? mlConfidence = null)
    {
        return new ConfidenceScore(value, ruleConfidence, mlConfidence);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
        if (RuleConfidence.HasValue) yield return RuleConfidence.Value;
        if (MlConfidence.HasValue) yield return MlConfidence.Value;
    }

    public override string ToString() => $"{Value:P0} ({Level})";
}
