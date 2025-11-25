using System.Diagnostics;
using System.Text;

namespace UserManagmentApi.Middleware
{
    public class HttpLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpLoggingMiddleware> _logger;
        private readonly IConfiguration _configuration;

        public HttpLoggingMiddleware(RequestDelegate next, ILogger<HttpLoggingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for static files and health checks
            if (ShouldSkipLogging(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString()[..8];

            // Log incoming request
            await LogRequestAsync(context.Request, requestId);

            // Capture original response body stream
            var originalBodyStream = context.Response.Body;

            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                // Process the request
                await _next(context);

                // Log outgoing response
                await LogResponseAsync(context.Response, requestId, stopwatch.ElapsedMilliseconds);

                // Copy response back to original stream
                await responseBody.CopyToAsync(originalBodyStream);
            }
            finally
            {
                context.Response.Body = originalBodyStream;
                stopwatch.Stop();
            }
        }

        private async Task LogRequestAsync(HttpRequest request, string requestId)
        {
            var requestInfo = new StringBuilder();
            requestInfo.AppendLine($"[{requestId}] === INCOMING REQUEST ===");
            requestInfo.AppendLine($"Method: {request.Method}");
            requestInfo.AppendLine($"Path: {request.Path}{request.QueryString}");
            requestInfo.AppendLine($"Protocol: {request.Protocol}");
            requestInfo.AppendLine($"Host: {request.Host}");
            requestInfo.AppendLine($"User-Agent: {request.Headers.UserAgent}");
            requestInfo.AppendLine($"Content-Type: {request.ContentType}");
            requestInfo.AppendLine($"Content-Length: {request.ContentLength}");

            // Log headers (excluding sensitive ones)
            requestInfo.AppendLine("Headers:");
            foreach (var header in request.Headers.Where(h => !IsSensitiveHeader(h.Key)))
            {
                requestInfo.AppendLine($"  {header.Key}: {header.Value}");
            }

            // Log request body for POST/PUT requests if enabled
            if (ShouldLogRequestBody(request) && request.ContentLength > 0)
            {
                request.EnableBuffering();
                var bodyAsText = await ReadStreamAsync(request.Body);
                request.Body.Position = 0;
                
                if (!string.IsNullOrEmpty(bodyAsText))
                {
                    requestInfo.AppendLine($"Body: {bodyAsText}");
                }
            }

            _logger.LogInformation(requestInfo.ToString());
        }

        private async Task LogResponseAsync(HttpResponse response, string requestId, long elapsedMs)
        {
            var responseInfo = new StringBuilder();
            responseInfo.AppendLine($"[{requestId}] === OUTGOING RESPONSE ===");
            responseInfo.AppendLine($"Status Code: {response.StatusCode}");
            responseInfo.AppendLine($"Content-Type: {response.ContentType}");
            responseInfo.AppendLine($"Content-Length: {response.ContentLength}");
            responseInfo.AppendLine($"Elapsed Time: {elapsedMs}ms");

            // Log response headers (excluding sensitive ones)
            responseInfo.AppendLine("Headers:");
            foreach (var header in response.Headers.Where(h => !IsSensitiveHeader(h.Key)))
            {
                responseInfo.AppendLine($"  {header.Key}: {header.Value}");
            }

            // Log response body if enabled and it's a reasonable size
            if (ShouldLogResponseBody(response) && response.Body.Length > 0)
            {
                response.Body.Position = 0;
                var bodyAsText = await ReadStreamAsync(response.Body);
                response.Body.Position = 0;
                
                if (!string.IsNullOrEmpty(bodyAsText))
                {
                    var maxBodyLength = _configuration.GetValue<int>("Logging:Http:MaxResponseBodyLength", 1000);
                    var truncatedBody = bodyAsText.Length > maxBodyLength 
                        ? $"{bodyAsText[..maxBodyLength]}... (truncated)"
                        : bodyAsText;
                    
                    responseInfo.AppendLine($"Body: {truncatedBody}");
                }
            }

            _logger.LogInformation(responseInfo.ToString());
        }

        private static async Task<string> ReadStreamAsync(Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        private static bool ShouldSkipLogging(PathString path)
        {
            var pathsToSkip = new[]
            {
                "/favicon.ico",
                "/robots.txt",
                "/_framework",
                "/css/",
                "/js/",
                "/images/"
            };

            return pathsToSkip.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsSensitiveHeader(string headerName)
        {
            var sensitiveHeaders = new[]
            {
                "authorization",
                "x-api-key",
                "cookie",
                "set-cookie"
            };

            return sensitiveHeaders.Contains(headerName.ToLowerInvariant());
        }

        private bool ShouldLogRequestBody(HttpRequest request)
        {
            var enableRequestBodyLogging = _configuration.GetValue<bool>("Logging:Http:EnableRequestBodyLogging", false);
            
            if (!enableRequestBodyLogging)
                return false;

            var contentType = request.ContentType?.ToLowerInvariant();
            return contentType != null && (
                contentType.Contains("application/json") ||
                contentType.Contains("application/xml") ||
                contentType.Contains("text/")
            );
        }

        private bool ShouldLogResponseBody(HttpResponse response)
        {
            var enableResponseBodyLogging = _configuration.GetValue<bool>("Logging:Http:EnableResponseBodyLogging", false);
            
            if (!enableResponseBodyLogging)
                return false;

            var contentType = response.ContentType?.ToLowerInvariant();
            return contentType != null && (
                contentType.Contains("application/json") ||
                contentType.Contains("application/xml") ||
                contentType.Contains("text/")
            );
        }
    }

    // Extension method to make it easier to register the middleware
    public static class HttpLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomHttpLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpLoggingMiddleware>();
        }
    }
}