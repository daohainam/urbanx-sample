# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["UrbanX.sln", "./"]
COPY ["src/ServiceDefaults/UrbanX.ServiceDefaults/UrbanX.ServiceDefaults.csproj", "src/ServiceDefaults/UrbanX.ServiceDefaults/"]
COPY ["src/Shared/UrbanX.Shared.Security/UrbanX.Shared.Security.csproj", "src/Shared/UrbanX.Shared.Security/"]
COPY ["src/Services/Order/UrbanX.Services.Order/UrbanX.Services.Order.csproj", "src/Services/Order/UrbanX.Services.Order/"]

# Restore dependencies
RUN dotnet restore "src/Services/Order/UrbanX.Services.Order/UrbanX.Services.Order.csproj"

# Copy all source files
COPY . .

# Build the project
WORKDIR "/src/src/Services/Order/UrbanX.Services.Order"
RUN dotnet build "UrbanX.Services.Order.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "UrbanX.Services.Order.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN groupadd -r urbanx && useradd -r -g urbanx urbanx
USER urbanx

COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/alive || exit 1

ENTRYPOINT ["dotnet", "UrbanX.Services.Order.dll"]
