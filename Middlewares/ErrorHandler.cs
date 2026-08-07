using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RoomBooking.Api.Middlewares;

/// <summary>
/// Central factory for the booking API's RFC 7807 problem-details error contract.
/// Every status maps to a ProblemDetails instance so the API returns one consistent shape.
/// 500 (fault) is produced by the problem-details middleware for unhandled exceptions.
/// </summary>
internal static class ErrorHandler
{
    /// <summary>400 — one or more request fields failed validation.</summary>
    public static ValidationProblemDetails Validation(ModelStateDictionary modelState) => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Validation error",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Detail = "One or more request fields failed validation. See the errors collection for details.",
        Errors = modelState.ToDictionary(
            e => e.Key,
            e => e.Value?.Errors.Select(err => err.ErrorMessage ?? string.Empty).ToArray() ?? [])
    };

    /// <summary>403 — the caller is not allowed to perform this action.</summary>
    public static ProblemDetails Forbidden(string detail) => new()
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Forbidden",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        Detail = detail
    };

    /// <summary>404 — the requested resource does not exist.</summary>
    public static ProblemDetails NotFound(int id, string resource) => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Resource not found",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        Detail = $"{resource} with id `{id}` was not found."
    };

    /// <summary>409 — the request conflicts with the current state of the resource.</summary>
    public static ProblemDetails Conflict(string detail) => new()
    {
        Status = StatusCodes.Status409Conflict,
        Title = "Conflict",
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        Detail = detail
    };
}
