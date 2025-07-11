using AIApiTracer.Models;
using AIApiTracer.Services;
using System.Diagnostics;
using System.Text;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;
using Microsoft.AspNetCore.Http.Extensions;

namespace AIApiTracer.Transformers;

public class ApiTraceTransformer : ITransformProvider
{
    private readonly IApiTraceService _apiTraceService;

    public ApiTraceTransformer(IApiTraceService apiTraceService)
    {
        _apiTraceService = apiTraceService;
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        // The actual tracing is handled by ApiTraceMiddleware
        // This transformer is kept for compatibility but does nothing
    }
}