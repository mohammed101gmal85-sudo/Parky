
using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Parky.API.Filters;

public sealed class FluentValidationActionFilter : IAsyncActionFilter, IOrderedFilter
{
    private static readonly MethodInfo ValidateAsyncMethod =
        typeof(FluentValidationActionFilter).GetMethod(
            nameof(ValidateAsyncGeneric),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    public int Order => -3000;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var actionArgument in context.ActionArguments)
        {
            var model = actionArgument.Value;

            if (model is null)
            {
                continue;
            }

            var modelType = model.GetType();

            var validatorType = typeof(IValidator<>)
                .MakeGenericType(modelType);

            var validator = context.HttpContext.RequestServices
                .GetService(validatorType);

            if (validator is null)
            {
                continue;
            }

            var genericMethod = ValidateAsyncMethod
                .MakeGenericMethod(modelType);

            var validationTask = (Task<ValidationResult>)genericMethod.Invoke(
                null,
                new object?[]
                {
                    validator,
                    model,
                    context.HttpContext.RequestAborted
                })!;

            var validationResult = await validationTask;

            foreach (var error in validationResult.Errors)
            {
                var key = string.IsNullOrWhiteSpace(error.PropertyName)
                    ? actionArgument.Key
                    : error.PropertyName;

                context.ModelState.AddModelError(
                    key,
                    error.ErrorMessage);
            }
        }

        await next();
    }

    private static Task<ValidationResult> ValidateAsyncGeneric<T>(
        object validator,
        object model,
        CancellationToken cancellationToken)
    {
        return ((IValidator<T>)validator).ValidateAsync(
            (T)model,
            cancellationToken);
    }
}
