# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for layer-cached restore
COPY ["UrbanX.sln", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["src/ServiceDefaults/UrbanX.ServiceDefaults/UrbanX.ServiceDefaults.csproj", "src/ServiceDefaults/UrbanX.ServiceDefaults/"]
COPY ["src/Shared/UrbanX.Shared.Security/UrbanX.Shared.Security.csproj", "src/Shared/UrbanX.Shared.Security/"]
COPY ["src/Management/UrbanX.Management.Web/UrbanX.Management.Web.csproj", "src/Management/UrbanX.Management.Web/"]

RUN dotnet restore "src/Management/UrbanX.Management.Web/UrbanX.Management.Web.csproj"

COPY . .
WORKDIR "/src/src/Management/UrbanX.Management.Web"
RUN dotnet publish "UrbanX.Management.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r -g 1001 urbanx && useradd -r -u 1001 -g urbanx urbanx

COPY --from=build --chown=urbanx:urbanx /app/publish .

USER 1001

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
  CMD curl --fail http://localhost:8080/alive || exit 1

ENTRYPOINT ["dotnet", "UrbanX.Management.Web.dll"]
