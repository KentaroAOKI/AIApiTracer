using AIApiTracer.Components;
using AIApiTracer.Middleware;
using AIApiTracer.Services;
using AIApiTracer.Services.MessageParsing;
using AIApiTracer.Services.Metadata;
using AIApiTracer.Services.Streaming;
using AIApiTracer.Transformers;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/json",
        "text/event-stream",
        "application/javascript",
        "text/css",
        "text/html",
        "text/xml",
        "text/plain",
        "application/xml",
        "application/xhtml+xml"
    });
});

// Configure compression providers
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Add API trace service
builder.Services.AddSingleton<IApiTraceService, ApiTraceService>();

// Add Header masking service
builder.Services.AddSingleton<IHeaderMaskingService, HeaderMaskingService>();

// Add SSE parsers
builder.Services.AddSingleton<ISseParser, AnthropicSseParser>();
builder.Services.AddSingleton<ISseParser, OpenAISseParser>();
builder.Services.AddSingleton<ISseParser, OpenAICompatSseParser>();
builder.Services.AddSingleton<ISseParserFactory, SseParserFactory>();

// Add AI metadata extractors
builder.Services.AddSingleton<IAiMetadataExtractor, AnthropicMetadataExtractor>();
builder.Services.AddSingleton<IAiMetadataExtractor, OpenAIMetadataExtractor>();
builder.Services.AddSingleton<IAiMetadataExtractor, XAIMetadataExtractor>();
builder.Services.AddSingleton<IAiMetadataExtractor, OpenAICompatMetadataExtractor>();
builder.Services.AddSingleton<IAiMetadataExtractorFactory, AiMetadataExtractorFactory>();

// Add message parsers
builder.Services.AddSingleton<IMessageParser, OpenAIMessageParser>();
builder.Services.AddSingleton<IMessageParser, AnthropicMessageParser>();
builder.Services.AddSingleton<IMessageParserFactory, MessageParserFactory>();

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms<ApiTraceTransformer>()
    .AddTransforms<AzureOpenAITransformer>()
    .AddTransforms<TargetUrlCaptureTransformer>()
    .AddTransforms<OpenAICompatTransformer>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

//app.UseHttpsRedirection();

// Add response compression
app.UseResponseCompression();

// Add API trace middleware before other middleware
app.UseMiddleware<ApiTraceMiddleware>();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map reverse proxy
app.MapReverseProxy();

app.Run();
