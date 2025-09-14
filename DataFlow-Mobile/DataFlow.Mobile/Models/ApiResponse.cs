using System.Net;

namespace DataFlow.Mobile.Models;

public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }
    public HttpStatusCode StatusCode { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public TimeSpan ResponseTime { get; set; }

    public static ApiResponse<T> Success(T data, HttpStatusCode statusCode = HttpStatusCode.OK, TimeSpan responseTime = default)
    {
        return new ApiResponse<T>
        {
            IsSuccess = true,
            Data = data,
            StatusCode = statusCode,
            ResponseTime = responseTime
        };
    }

    public static ApiResponse<T> Error(string errorMessage, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
    {
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            StatusCode = statusCode
        };
    }
}