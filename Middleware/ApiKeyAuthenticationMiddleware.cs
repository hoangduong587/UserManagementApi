namespace UserManagmentApi.Middleware
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;
        private const string ApiKeyHeaderName = "X-API-Key";

        public ApiKeyAuthenticationMiddleware(
            RequestDelegate next, 
            IConfiguration configuration, 
            ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip authentication for Swagger/OpenAPI endpoints and health checks
            if (ShouldSkipAuthentication(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Extract API key from header
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                _logger.LogWarning("API key missing from request to {Path}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing");
                return;
            }

            // Validate API key
            var validApiKeys = _configuration.GetSection("Authentication:ApiKeys").Get<string[]>() ?? Array.Empty<string>();
            
            if (!validApiKeys.Any(key => key == extractedApiKey.ToString()))
            {
                _logger.LogWarning("Invalid API key attempted access to {Path}", context.Request.Path);
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid API Key");
                return;
            }

            _logger.LogInformation("Authenticated request to {Path} with valid API key", context.Request.Path);
            await _next(context);
        }

        private static bool ShouldSkipAuthentication(PathString path)
        {
            var pathsToSkip = new[]
            {
                "/swagger",
                "/openapi",
                "/health",
                "/favicon.ico"
            };

            return pathsToSkip.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
        }
    }

    // Extension method to make it easier to register the middleware
    public static class ApiKeyAuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKeyAuthentication(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyAuthenticationMiddleware>();
        }
    }
}