using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace AIApiTracer.Transformers;

public class OpenAICompatTransformer : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext transformBuilderContext)
    {
        if (transformBuilderContext.Route.RouteId == "openai-compat-route")
        {
            transformBuilderContext.AddRequestTransform(transformContext =>
            {
                var httpContext = transformContext.HttpContext;
                var path = httpContext.Request.Path.Value;
                
                if (path != null && path.StartsWith("/endpoint/openai-compat/", StringComparison.OrdinalIgnoreCase))
                {
                    // Extract the destination URL from the path
                    // Format: /endpoint/openai-compat/{full-url}
                    // Example: /endpoint/openai-compat/http://localhost:12345/v1/chat/completions
                    var remainingPath = path.Substring("/endpoint/openai-compat/".Length);
                    
                    // The remaining path should be a complete URL
                    if (Uri.TryCreate(remainingPath, UriKind.Absolute, out var destinationUri))
                    {
                        var builder = new UriBuilder(destinationUri);
                        
                        // Add query string if present
                        if (httpContext.Request.QueryString.HasValue && !string.IsNullOrEmpty(httpContext.Request.QueryString.Value))
                        {
                            builder.Query = httpContext.Request.QueryString.Value;
                        }
                        
                        // Update the destination
                        transformContext.ProxyRequest.RequestUri = builder.Uri;
                        
                        // Store target URL for tracing
                        httpContext.Items["ApiTrace_TargetUrl"] = builder.Uri.ToString();
                        
                        // Store endpoint type for SSE parser selection
                        httpContext.Items["ApiTrace_EndpointType"] = "OpenAICompat";
                    }
                }
                
                return ValueTask.CompletedTask;
            });
        }
    }
}