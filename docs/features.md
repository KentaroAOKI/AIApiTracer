# Key Features

## 1. Reverse Proxy
- Proxies requests to multiple AI APIs using YARP
- Dynamic routing support for Azure OpenAI (generates URLs from resource names)

## 2. Request/Response Tracing
- Captures all API requests and responses
- Records headers, body, status codes, and execution time
- Stores up to 1000 traces in-memory

## 3. Security Features
- Automatic API key masking (e.g., sk-foo******qux)
- Masks headers like Authorization, api-key, x-api-key

## 4. Response Processing
- Automatic decompression based on Content-Encoding (gzip, deflate, brotli)
- Automatic detection and parsing of SSE responses
- Merges Anthropic API SSE responses into a single JSON

## 5. AI Metadata Extraction
- Automatically extracts model names from responses
- Displays token usage (input, output, total)
- Shows cached token count (for supported providers only)

## 6. UI Monitoring
- Real-time trace display (auto-refresh every 2 seconds)
- Color-coded endpoint type display
- Shows model name and token usage in list view
- Details modal for viewing all information
  - Tab switching between General (always visible), Request, and Response
  - Copy buttons for each section
  - Fixed height (90vh) scrollable layout
