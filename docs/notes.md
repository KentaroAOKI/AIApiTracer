# Development Notes

- Since Blazor Server is used, UI logic is executed server-side
- Azure OpenAI requires special processing to dynamically build URLs from resource names
- Sensitive information like API keys is automatically masked (Authorization, api-key, x-api-key headers, etc.)
- Response bodies are automatically decompressed based on Content-Encoding (supports gzip, deflate, brotli)
- SSE responses are automatically detected and appropriate parsers are applied
- SSE parsers and AI metadata extractors are selected based on endpoint type, not the destination URL
- **To add a new AI provider**:
  1. Add a new route to YARP configuration (`appsettings.json`)
  2. Create transformers as needed
  3. Implement `ISseParser` if there are SSE responses
  4. Implement `IAiMetadataExtractor` if metadata extraction is needed
  5. Register in DI container (`Program.cs`)
- When using invalid URL examples, use example.invalid instead of example.com
