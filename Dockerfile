# Build stage - Contains full SDK for compilation
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file
COPY ExchangeRateComparator.sln ./

# Copy project files (for dependency caching)
COPY src/ExchangeRateComparator.Domain/ExchangeRateComparator.Domain.csproj src/ExchangeRateComparator.Domain/
COPY src/ExchangeRateComparator.Application/ExchangeRateComparator.Application.csproj src/ExchangeRateComparator.Application/
COPY src/ExchangeRateComparator.Infrastructure/ExchangeRateComparator.Infrastructure.csproj src/ExchangeRateComparator.Infrastructure/
COPY src/ExchangeRateComparator.Console/ExchangeRateComparator.Console.csproj src/ExchangeRateComparator.Console/

# Restore dependencies (cached layer if .csproj files don't change)
RUN dotnet restore src/ExchangeRateComparator.Console/ExchangeRateComparator.Console.csproj

# Copy source code
COPY src/ src/

# Build and publish
WORKDIR /src/src/ExchangeRateComparator.Console
RUN dotnet publish -c Release -o /app/publish --no-restore \
    /p:PublishTrimmed=false \
    /p:PublishSingleFile=false

# Runtime stage - Minimal image with only runtime dependencies
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Create non-root user for security
RUN useradd -m -u 1000 appuser && \
    chown -R appuser:appuser /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Switch to non-root user
USER appuser

# Configure environment
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Health check (will exit successfully when console app completes)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD pgrep -f "ExchangeRateComparator.Console" || exit 1

# Set entry point
ENTRYPOINT ["dotnet", "ExchangeRateComparator.Console.dll"]
