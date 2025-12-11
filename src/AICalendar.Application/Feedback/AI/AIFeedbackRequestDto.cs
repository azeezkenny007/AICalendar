
namespace AICalendar.Application.Feedback.AI;

public record AiFeedbackRequestDto(
    Guid user_id,
    List<AiFeedbackItemDto> feedback_items
);

public record AiFeedbackItemDto(
    string prediction_id,
    string action,
    AiPredictionDetailsDto? prediction_details,
    string? discard_reason,
    AiEditedDataDto? edited_data
);

public record AiPredictionDetailsDto(
    string category,
    decimal predicted_amount
);

public record AiEditedDataDto(
    string corrected_title,
    decimal corrected_amount,
    DateTime corrected_date,
    string corrected_category
);
