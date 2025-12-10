using AICalendar.Domain.Common;

namespace AICalendar.Application.Common.Models;

/// <summary>
/// Extended result type that includes HTTP status context for API responses
/// </summary>
public class OperationResult : Result
{
    public int StatusCode { get; }
    public string? Title { get; }
    public string? Detail { get; }
    public object? Data { get; }

    protected OperationResult(
        bool isSuccess,
        int statusCode,
        string error,
        string? title = null,
        string? detail = null,
        object? data = null)
        : base(isSuccess, error)
    {
        StatusCode = statusCode;
        Title = title;
        Detail = detail;
        Data = data;
    }

    /// <summary>
    /// Creates a successful operation result (200 OK)
    /// </summary>
    public static OperationResult Success(string title, string message, object? data = null)
        => new(true, 200, string.Empty, title, message, data);

    /// <summary>
    /// Creates a not found error result (404 Not Found)
    /// </summary>
    public static OperationResult NotFound(string title, string error, string detail)
        => new(false, 404, error, title, detail, null);

    /// <summary>
    /// Creates a bad request error result (400 Bad Request)
    /// </summary>
    public static OperationResult BadRequest(string title, string error, string detail)
        => new(false, 400, error, title, detail, null);

    /// <summary>
    /// Creates a conflict error result (409 Conflict)
    /// </summary>
    public static OperationResult Conflict(string title, string error, string detail)
        => new(false, 409, error, title, detail, null);

    /// <summary>
    /// Creates a server error result (500 Internal Server Error)
    /// </summary>
    public static OperationResult ServerError(string title, string error, string detail)
        => new(false, 500, error, title, detail, null);
}

/// <summary>
/// Generic operation result with typed data
/// </summary>
public class OperationResult<T> : OperationResult
{
    public new T? Data { get; }

    private OperationResult(
        bool isSuccess,
        int statusCode,
        string error,
        T? data,
        string? title = null,
        string? detail = null)
        : base(isSuccess, statusCode, error, title, detail, data)
    {
        Data = data;
    }

    /// <summary>
    /// Creates a successful operation result with typed data (200 OK)
    /// </summary>
    public static OperationResult<T> Success(string title, string message, T data)
        => new(true, 200, string.Empty, data, title, message);

    /// <summary>
    /// Creates a not found error result (404 Not Found)
    /// </summary>
    public static new OperationResult<T> NotFound(string title, string error, string detail)
        => new(false, 404, error, default, title, detail);

    /// <summary>
    /// Creates a bad request error result (400 Bad Request)
    /// </summary>
    public static new OperationResult<T> BadRequest(string title, string error, string detail)
        => new(false, 400, error, default, title, detail);

    /// <summary>
    /// Creates a conflict error result (409 Conflict)
    /// </summary>
    public static new OperationResult<T> Conflict(string title, string error, string detail)
        => new(false, 409, error, default, title, detail);

    /// <summary>
    /// Creates a server error result (500 Internal Server Error)
    /// </summary>
    public static new OperationResult<T> ServerError(string title, string error, string detail)
        => new(false, 500, error, default, title, detail);
}
