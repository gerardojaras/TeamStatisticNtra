namespace TeamSearch.Shared;

public sealed class ApiError
{
    public string Code { get; set; } = "error";
    public string? Message { get; set; }
    public object? Details { get; set; }
}

public sealed class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public ApiError? Error { get; set; }
    public T? Data { get; set; }
    public object? Meta { get; set; }

    public static ApiResponse<T> SuccessResponse(T? data, object? meta = null, string? message = null)
    {
        return new ApiResponse<T> { Success = true, Data = data, Meta = meta, Message = message };
    }

    public static ApiResponse<T> Failure(string code = "error", string? message = null, object? details = null)
    {
        return new ApiResponse<T>
        {
            Success = false, Error = new ApiError { Code = code, Message = message, Details = details },
            Message = message
        };
    }
}