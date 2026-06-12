namespace BookSwap.Web.Services;

public enum ServiceError { None, NotFound, Forbidden, Invalid, Conflict }

public class ServiceResult
{
    public ServiceError Error { get; init; } = ServiceError.None;
    public string? Message { get; init; }
    public bool Ok => Error == ServiceError.None;

    public static ServiceResult Success() => new();
    public static ServiceResult Fail(ServiceError error, string? message = null) => new() { Error = error, Message = message };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }

    public static ServiceResult<T> Success(T value) => new() { Value = value };
    public static new ServiceResult<T> Fail(ServiceError error, string? message = null) => new() { Error = error, Message = message };
}
