using ErrorOr;

namespace WebApi.Extensions;

internal static class ErrorOrExtension
{
    internal static IResult ToResult<T>(
        this ErrorOr<T> result)
    {
        return result.Match(
            value => Results.Ok(value),
            errors => errors.All(e => e.Type is ErrorType.Validation)
                ? ToValidationProblem(errors)
                : ToProblem(errors));
    }

    private static IResult ToValidationProblem(List<Error> errors)
    {
        var validationErrors = errors
            .GroupBy(e => e.Code)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.Description).ToArray());

        return Results.ValidationProblem(
            validationErrors,
            title: "One or more validation errors occurred.",
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static IResult ToProblem(List<Error> errors)
    {
        var firstError = errors[0];

        var statusCode = firstError.Type switch
        {
            ErrorType.Failure => StatusCodes.Status400BadRequest,
            ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: firstError.Description,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = errors.Select(e => new
                {
                    code = e.Code,
                    description = e.Description
                }).ToArray()
            });
    }
}
