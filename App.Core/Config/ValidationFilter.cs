using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;
using App.Core.Base;
using System.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace App.Core.Config
{
    public class ValidationFilter : IAsyncActionFilter
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidationFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var argumentType = argument.GetType();

                if (argument is IEnumerable collection && argumentType.IsGenericType)
                {
                    var elementType = argumentType.GetGenericArguments()[0];
                    var validatorType = typeof(IValidator<>).MakeGenericType(elementType);
                    var validator = _serviceProvider.GetService(validatorType) as IValidator;  

                    if (validator != null)
                    {
                        var failures = new List<ValidationFailure>();
                        int index = 0;
                        foreach (var item in collection)
                        {
                            if (item == null) continue; 

                            var result = await validator.ValidateAsync(new ValidationContext<object>(item));
                            if (!result.IsValid)
                                failures.AddRange(result.Errors.Select(
                                    f => new ValidationFailure(
                                        $"[{index}].{f.PropertyName}", f.ErrorMessage)));
                            index++;
                        }

                        if (failures.Any())
                            throw new Base.ValidationException(failures);
                    }
                }
                else
                {
                    var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
                    var validator = _serviceProvider.GetService(validatorType) as IValidator; // Resolve and cast

                    if (validator != null)
                    {
                        var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
                        if (!result.IsValid)
                            throw new Base.ValidationException(result.Errors);
                    }
                }
            }

            await next();
        }
    }
}