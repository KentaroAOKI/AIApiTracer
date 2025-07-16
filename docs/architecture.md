# Architecture Overview

## Project Structure
```
AIApiTracer/
├── src/
│   └── AIApiTracer/
│       ├── Components/
│       │   ├── Controls/	Custom controls
│       │   ├── Layout/	Page layouts
│       │   └── Pages/	Pages
│       ├── Middleware/	ASP.NET Core middleware
│       ├── Models/
│       ├── Services/	Various services
│       │   ├── Metadata/	AI metadata related
│       │   ├── MessageParsing/	AI message parsing
│       │   ├── Streaming/	Streaming response processing
│       ├── Styles/	Web styles (source for Tailwind CSS build)
│       ├── Transformers/	YARP transformers
│       └── wwwroot/
│           ├── css/
│           └── fonts/	Web Fonts (mainly Fluent System Icons)
└── test/
    └── AIApiTracer.Tests/
        ├── Resources/
        └── Services/
```

## Major Components

1. **Program.cs**: 
   - Configures Blazor Server components and interactive server rendering
   - YARP reverse proxy configuration
   - DI registration for various services (ApiTraceService, HeaderMaskingService, SSE parsers, AI metadata extractors)
   - Registration of various transformers
   - Response compression middleware configuration (Brotli, Gzip)

2. **Middleware** (under `Middleware/`):
   - `ApiTraceMiddleware`: Integrates request/response capture, header masking, SSE parsing, and AI metadata extraction
     - Identifies endpoint type from URL path and selects appropriate SSE parser and metadata extractor
   - `StreamCapturingStream`: Captures response stream and automatically decompresses based on Content-Encoding

3. **YARP Transformers** (under `Transformers/`): 
   - `ApiTraceTransformer`: Request/response capture and recording
   - `AzureOpenAITransformer`: Dynamic URL transformation for Azure OpenAI, endpoint type configuration
   - `TargetUrlCaptureTransformer`: Target URL recording
   - `OpenAICompatTransformer`: Dynamic URL transformation for OpenAI-compatible endpoints, endpoint type configuration

4. **API Trace Features**:
   - `ApiTraceRecord`: Model for trace data (headers, body, status, execution time, AI metadata, etc.)
   - `ApiTraceService`: Manages trace data in-memory (max 1000 records)
   - `HeaderMaskingService`: Masks sensitive information like API keys (e.g., sk-foo*****qux)

5. **SSE Response Processing**:
   - `ISseParser`/`SseParserFactory`: Extensible SSE parser system
   - `AnthropicSseParser`: Merges Anthropic API SSE responses into a single JSON (supports tool_use blocks)
   - `OpenAISseParser`: Parses OpenAI/Azure OpenAI API SSE responses (supports tool_calls)
   - `OpenAICompatSseParser`: Handles OpenAI-compatible API SSE responses (uses OpenAISseParser internally)

6. **AI Metadata Extraction**:
   - `AiMetadata`: Stores model name, token usage, cache information, and rate limit data
   - Extractors for each provider (Anthropic, OpenAI/Azure OpenAI, xAI, OpenAI-compatible)
   - Automatically extracts model name, token usage, and rate limit information from responses

7. **Message Parsing**:
   - `IMessageParser`/`MessageParserFactory`: Extensible message parser system
   - `OpenAIMessageParser`: Parses OpenAI-style messages (supports tool_calls, multimodal content)
   - `AnthropicMessageParser`: Parses Anthropic-style messages (supports tool_use/tool_result blocks)
   - `ParsedMessage`: Unified message model with role, content, content parts, and tool calls

8. **Blazor Components**: 
   - `Home.razor`: Real-time display of trace data (auto-refresh every 2 seconds), shows model name and token usage
     - Click row to expand (displays request/response body inline)
     - Assigns unique ID to each request using Guid.CreateVersion7()
   - `Controls/Tabs.razor`/`Tab.razor`: Reusable tab UI components
   - `Controls/CopyButton.razor`: Button component with clipboard copy functionality (shows "Copied!" feedback after copying)
   - `Controls/MessageDisplay.razor`: Displays parsed messages with tool calls/results
   - `Controls/ToolCallDisplay.razor`: Reusable component for displaying tool invocations (blue style)
   - `Controls/ToolResultDisplay.razor`: Reusable component for displaying tool results (green style)
   - Color-coded endpoint type display
   - Details modal allows viewing headers, body, AI metadata, and parsed messages (using tab UI)

## Reverse Proxy Endpoints

- `/endpoint/openai/{path}` → `https://api.openai.com/{path}`
- `/endpoint/anthropic/{path}` → `https://api.anthropic.com/{path}`
- `/endpoint/x/{path}` → `https://api.x.ai/{path}`
- `/endpoint/aoai/{resource-name}/{deployment-name}` → `https://{resource-name}.openai.azure.com/{deployment-name}`
- `/endpoint/openai-compat/{full-url}` → `{full-url}` (for OpenAI-compatible APIs)
