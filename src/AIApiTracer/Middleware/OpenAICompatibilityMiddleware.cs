using AIApiTracer.Models;
using Microsoft.Extensions.Options;

namespace AIApiTracer.Middleware;

public class OpenAICompatibilityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<AIApiTracerOptions> _options;

    public OpenAICompatibilityMiddleware(RequestDelegate next, IOptionsMonitor<AIApiTracerOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if the request is for OpenAI compatibility endpoint
        if (context.Request.Path.StartsWithSegments("/endpoint/openai-compat"))
        {
            // If OpenAI compatibility forwarding is disabled, return 404
            if (!_options.CurrentValue.EnableOpenAICompatForwarding)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("OpenAI compatibility endpoint is disabled.");
                return;
            }
        }

        await _next(context);
    }
}