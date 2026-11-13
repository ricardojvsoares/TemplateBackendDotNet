using ErrorOr;

namespace WebApi.Extensions;

internal static class ErrorOrExtension
{
    internal static IResult ToResult<T>(this ErrorOr<T> result) =>
        result.ToResult(value => Results.Ok(value));

    internal static IResult ToResult<T>(
        this ErrorOr<T> result,
        Func<T, IResult> onSuccess)
    {
        return result.Match(
            onSuccess,
            errors => errors.All(e => e.Type is ErrorType.Validation)
                ? ToValidationProblem(errors)
                : ToProblem(errors));
    }

    internal static IResult ToCreatedResult<T>(this ErrorOr<T> result) =>
        result.ToResult(value => Results.Json(value, statusCode: StatusCodes.Status201Created));

    internal static IResult ToAcceptedResult<T>(this ErrorOr<T> result) =>
        result.ToResult(value => Results.Json(value, statusCode: StatusCodes.Status202Accepted));

    internal static IResult ToNoContentResult<T>(this ErrorOr<T> result) =>
        result.ToResult(_ => Results.NoContent());

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
