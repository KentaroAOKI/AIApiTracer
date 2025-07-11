using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace AIApiTracer.Transformers;

public class AzureOpenAITransformer : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.RouteId == "azure-openai-route")
        {
            context.AddRequestTransform(transformContext =>
            {
                var httpContext = transformContext.HttpContext;
                var path = httpContext.Request.Path.Value;
                
                if (string.IsNullOrEmpty(path))
                    return ValueTask.CompletedTask;
                
                // Extract resource name from path: /endpoint/aoai/{resource-name}/{deployment-name}
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 3 && segments[0] == "endpoint" && segments[1] == "aoai")
                {
                    var resourceName = segments[2];
                    var remainingPath = string.Join('/', segments.Skip(3));
                    
                    // Update the destination
                    var destinationPrefix = $"https://{resourceName}.openai.azure.com/";
                    var targetUri = new Uri(destinationPrefix + remainingPath + httpContext.Request.QueryString);
                    transformContext.ProxyRequest.RequestUri = targetUri;
                    
                    // Set the host header
                    transformContext.ProxyRequest.Headers.Host = $"{resourceName}.openai.azure.com";
                    
                    // Store target URL for tracing
                    httpContext.Items["ApiTrace_TargetUrl"] = targetUri.ToString();
                    
                    // Store endpoint type for SSE parser selection
                    httpContext.Items["ApiTrace_EndpointType"] = "AzureOpenAI";
                }
                
                return ValueTask.CompletedTask;
            });
        }
    }
}