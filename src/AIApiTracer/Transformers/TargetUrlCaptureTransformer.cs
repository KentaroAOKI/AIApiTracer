using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace AIApiTracer.Transformers;

public class TargetUrlCaptureTransformer : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        // Add a transform that runs after URL transformation to capture the final target URL
        context.AddRequestTransform(transformContext =>
        {
            var httpContext = transformContext.HttpContext;
            
            // Skip if target URL is already set (e.g., by AzureOpenAITransformer)
            if (httpContext.Items.ContainsKey("ApiTrace_TargetUrl"))
            {
                return ValueTask.CompletedTask;
            }
            
            // Get the destination prefix and combine with the path
            var destinationPrefix = transformContext.DestinationPrefix;
            var path = transformContext.Path.Value ?? "";
            var queryString = transformContext.Query.QueryString;
            
            if (!string.IsNullOrEmpty(destinationPrefix))
            {
                var targetUrl = destinationPrefix.TrimEnd('/') + "/" + path.TrimStart('/') + queryString;
                httpContext.Items["ApiTrace_TargetUrl"] = targetUrl;
            }
            
            return ValueTask.CompletedTask;
        });
    }
}