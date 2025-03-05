using System.Diagnostics;

namespace ERPApplication.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _logger = logger;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            _logger.LogInformation("------ Request Start ------");
            _logger.LogInformation("Request URL: {Url}", context.Request.Path);
            _logger.LogInformation("Method Type: {Method}", context.Request.Method);

            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            _logger.LogInformation("------ Request End ------");
            _logger.LogInformation("Request URL: {Url}", context.Request.Path);
            _logger.LogInformation("Method Type: {Method}", context.Request.Method);
            _logger.LogInformation("Status: {StatusCode}", context.Response.StatusCode);
            _logger.LogInformation("Execution Time: {ElapsedMilliseconds} ms", stopwatch.ElapsedMilliseconds);
        }
    }
}
