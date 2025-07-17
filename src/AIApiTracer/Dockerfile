# Base image with multi-platform support
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage with multi-platform support
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG TARGETARCH
WORKDIR /src

# Copy everything
COPY . ./

# Map Docker platform to .NET RID
RUN if [ "$TARGETARCH" = "amd64" ]; then \
        RID="linux-x64"; \
    elif [ "$TARGETARCH" = "arm64" ]; then \
        RID="linux-arm64"; \
    elif [ "$TARGETARCH" = "arm" ]; then \
        RID="linux-arm"; \
    else \
        echo "Unsupported architecture: $TARGETARCH"; \
        exit 1; \
    fi && \
    dotnet publish -a $TARGETARCH -o /app/publish src/AIApiTracer/AIApiTracer.csproj

# Build runtime image
FROM --platform=$TARGETPLATFORM base AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AIApiTracer.dll"]