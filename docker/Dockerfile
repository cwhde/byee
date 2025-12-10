# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy csproj and restore (for layer caching)
COPY src/Byee.Server/Byee.Server.csproj ./src/Byee.Server/
RUN dotnet restore src/Byee.Server/Byee.Server.csproj

# Copy everything else and build
COPY . .
RUN dotnet publish src/Byee.Server/Byee.Server.csproj -c Release -o /app/publish \
    --no-restore \
    -p:PublishSingleFile=false \
    -p:PublishTrimmed=false

# Runtime stage - use alpine for smallest image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install ca-certificates for HTTPS and curl for healthcheck
RUN apk add --no-cache ca-certificates tzdata curl

# Create non-root user
RUN addgroup -g 1000 byee && \
    adduser -u 1000 -G byee -s /bin/sh -D byee

# Create data directories with proper permissions
RUN mkdir -p /app/data/files && chown -R byee:byee /app/data

# Copy published app
COPY --from=build /app/publish .

# Set ownership
RUN chown -R byee:byee /app

USER byee

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV BYEE_STORAGE_PATH=/app/data
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Byee.Server.dll"]
