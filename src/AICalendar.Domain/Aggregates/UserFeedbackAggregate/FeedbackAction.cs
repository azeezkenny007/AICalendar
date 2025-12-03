public enum FeedbackAction
{
    /// <summary>
    /// User accepted the prediction item
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// User rejected the prediction item
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// User edited then accepted (indicates prediction was close but not perfect)
    /// </summary>
    EditedThenAccepted = 3
}