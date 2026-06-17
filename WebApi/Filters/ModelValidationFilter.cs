using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WebApi.Models.Responses.Base;

namespace WebApi.Filters;

public static class ModelValidationFilter
{
    public static IActionResult ValidateRequest(ActionContext context)
    {
        var validates = new List<ValidateRes>();

        foreach (var state in context.ModelState)
        {
            if (state.Value.ValidationState != ModelValidationState.Invalid) continue;

            validates.Add(new ValidateRes
            {
                Key = state.Key,
                Errors = state.Value.Errors.Select(i => i.ErrorMessage).ToArray()
            });
        }

        return new BadRequestObjectResult(new ExecutionRes
        {
            Success = false,
            Error = "Invalid input",
            Validates = [.. validates]
        });
    }
}