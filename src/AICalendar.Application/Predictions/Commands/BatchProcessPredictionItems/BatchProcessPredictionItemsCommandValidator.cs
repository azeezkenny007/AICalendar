using FluentValidation;

namespace AICalendar.Application.Predictions.Commands.BatchProcessPredictionItems;

public class BatchProcessPredictionItemsCommandValidator : AbstractValidator<BatchProcessPredictionItemsCommand>
{
    public BatchProcessPredictionItemsCommandValidator()
    {
        RuleFor(v => v.AcceptedItemIds)
            .NotNull()
            .WithMessage("Accepted items list cannot be null.");

        RuleFor(v => v.RejectedItemIds)
            .NotNull()
            .WithMessage("Rejected items list cannot be null.");

        // Fix for BUG-06: Limit to max 10 items total
        RuleFor(x => x)
            .Must(x => (x.AcceptedItemIds.Count + x.RejectedItemIds.Count) <= 10)
            .WithMessage("Cannot process more than 10 items in a single batch.");

        // Rule: At least one item required (Option B)
        RuleFor(x => x)
            .Must(x => (x.AcceptedItemIds.Count + x.RejectedItemIds.Count) > 0)
            .WithMessage("At least one item must be provided (accepted or rejected).");

        // Fix for BUG-06: Check for duplicates
        RuleFor(v => v.AcceptedItemIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate items found in accepted list.");

        RuleFor(v => v.RejectedItemIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate items found in rejected list.");

        // Ensure no item is in both lists
        RuleFor(x => x)
            .Must(x => !x.AcceptedItemIds.Intersect(x.RejectedItemIds).Any())
            .WithMessage("The same item cannot be both accepted and rejected.");
    }
}
