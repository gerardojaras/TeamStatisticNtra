namespace TeamSearch.Shared;

public sealed class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public T? Data { get; set; }
    public object? Meta { get; set; }

    public static ApiResponse<T> SuccessResponse(T? data, object? meta = null, string? message = null)
        => new() { Success = true, Data = data, Meta = meta, Message = message };

    public static ApiResponse<T> Failure(string? message = null, object? meta = null)
        => new() { Success = false, Message = message, Meta = meta };
}

