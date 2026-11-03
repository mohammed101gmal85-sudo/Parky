using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parky.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    public ApiResponse()
    {
    }

    public ApiResponse(bool success, string message, T? data)
    {
        Success = success;
        Message = message;
        Data = data;
    }

    public static ApiResponse<T> SuccessResponse(
        T? data,
        string message = "Request completed successfully.")
    {
        return new ApiResponse<T>(
            true,
            message,
            data);
    }

    public static ApiResponse<T> FailureResponse(
        string message)
    {
        return new ApiResponse<T>(
            false,
            message,
            default);
    }
}
