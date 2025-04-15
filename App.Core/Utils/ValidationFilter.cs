using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace App.Core.Utils
{
    public class ValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var failures = context.ModelState
                    .Where(x => x.Value?.Errors?.Count > 0)
                    .SelectMany(x => x.Value!.Errors!
                        .Select(e => new FluentValidation.Results.ValidationFailure(x.Key, e.ErrorMessage)))
                    .ToList();

                throw new Core.Base.ValidationException(failures);
            }

            await next();
        }
    }
}