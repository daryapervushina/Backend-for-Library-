using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Library.Filters;

public class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.ModelState.IsValid)
        {
            return;
        }

        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry =>
                {
                    var message = entry.Value!.Errors[0].ErrorMessage;
                    return string.IsNullOrEmpty(message) ? "invalid value" : message;
                });

        context.Result = new BadRequestObjectResult(errors);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
