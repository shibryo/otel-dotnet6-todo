using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Domain.Exceptions;

namespace TodoApp.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("/error")]
    public IActionResult HandleError()
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
        
        if (exception == null)
        {
            return Problem();
        }

        _logger.LogError(exception, "An error occurred: {Message}", exception.Message);

        return exception switch
        {
            TodoDomainException => Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Domain validation error",
                detail: exception.Message
            ),
            FluentValidation.ValidationException validationEx => ValidationProblem(
                new ValidationProblemDetails(
                    validationEx.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        )
                )
            ),
            _ => Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "An unexpected error occurred",
                detail: "Please try again later"
            )
        };
    }
}
