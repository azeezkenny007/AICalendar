using AICalendar.APP.Common.Models;
using FluentValidation;
using MediatR;

namespace AICalendar.APP.Common.Behaviours;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(x => x.ValidateAsync(context, cancellationToken)));

        var failures = validationResults.SelectMany(x => x.Errors).Where(f => f != null).ToList();

        if (failures.Any())
        {
            var resultType = typeof(TResponse);
            if (resultType.IsGenericType)
            {
                var genericType = resultType.GetGenericArguments()[0];
                var failureMethod = typeof(Result<>).MakeGenericType(genericType).GetMethod(nameof(Result<object>.Failure));

                var errors = failures.Select(f => f.ErrorMessage).ToArray();
                return (TResponse)failureMethod!.Invoke(null, new object[] { errors })!;
            }
        }
        return await next();
    }
}
