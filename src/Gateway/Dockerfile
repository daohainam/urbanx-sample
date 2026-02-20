# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for layer-cached restore
COPY ["UrbanX.sln", "./"]
COPY ["src/ServiceDefaults/UrbanX.ServiceDefaults/UrbanX.ServiceDefaults.csproj", "src/ServiceDefaults/UrbanX.ServiceDefaults/"]
COPY ["src/Gateway/UrbanX.Gateway/UrbanX.Gateway.csproj", "src/Gateway/UrbanX.Gateway/"]

# Restore dependencies
RUN dotnet restore "src/Gateway/UrbanX.Gateway/UrbanX.Gateway.csproj"

# Copy all source files and publish
COPY . .
WORKDIR "/src/src/Gateway/UrbanX.Gateway"
RUN dotnet publish "UrbanX.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for security
RUN groupadd -r -g 1001 urbanx && useradd -r -u 1001 -g urbanx urbanx

COPY --from=build --chown=urbanx:urbanx /app/publish .

USER 1001

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl --fail http://localhost:8080/alive || exit 1

ENTRYPOINT ["dotnet", "UrbanX.Gateway.dll"]
