using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyApp.Api.Common;

namespace MyApp.Api.Filters;

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();

            var response = new ApiResponse<object>
            {
                Success = false,
                StatusCode = 400,
                Message = "Validation failed.",
                Errors = errors
            };

            context.Result = new BadRequestObjectResult(response);
        }
    }
}
