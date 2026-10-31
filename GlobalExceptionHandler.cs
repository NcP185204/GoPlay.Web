using Microsoft.AspNetCore.Diagnostics;

namespace GoPlay.Web;

// Bắt toàn bộ exception chưa xử lý ở tầng pipeline.
// Ghi log lỗi kèm HTTP method + path, sau đó chuyển hướng người dùng về /Home/Error.
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.Redirect("/Home/Error");
        return ValueTask.FromResult(true);
    }
}