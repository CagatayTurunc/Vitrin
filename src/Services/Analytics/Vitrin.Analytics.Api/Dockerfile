# ==============================================================
# Vitrin Analytics Service - Multi-Stage Dockerfile
# SQLite veritabanı /app/data dizininde saklanır (volume mount)
# ==============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/Vitrin.Shared.Kernel/Vitrin.Shared.Kernel.csproj",                               "src/Shared/Vitrin.Shared.Kernel/"]
COPY ["src/Shared/Vitrin.Shared.Contracts/Vitrin.Shared.Contracts.csproj",                         "src/Shared/Vitrin.Shared.Contracts/"]
COPY ["src/Shared/Vitrin.Shared.Infrastructure/Vitrin.Shared.Infrastructure.csproj",               "src/Shared/Vitrin.Shared.Infrastructure/"]

COPY ["src/Services/Analytics/Vitrin.Analytics.Domain/Vitrin.Analytics.Domain.csproj",             "src/Services/Analytics/Vitrin.Analytics.Domain/"]
COPY ["src/Services/Analytics/Vitrin.Analytics.Application/Vitrin.Analytics.Application.csproj",   "src/Services/Analytics/Vitrin.Analytics.Application/"]
COPY ["src/Services/Analytics/Vitrin.Analytics.Infrastructure/Vitrin.Analytics.Infrastructure.csproj", "src/Services/Analytics/Vitrin.Analytics.Infrastructure/"]
COPY ["src/Services/Analytics/Vitrin.Analytics.Api/Vitrin.Analytics.Api.csproj",                   "src/Services/Analytics/Vitrin.Analytics.Api/"]

RUN dotnet restore "src/Services/Analytics/Vitrin.Analytics.Api/Vitrin.Analytics.Api.csproj"

COPY . .
RUN dotnet publish "src/Services/Analytics/Vitrin.Analytics.Api/Vitrin.Analytics.Api.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /app/data
RUN adduser --disabled-password --gecos "" appuser
RUN chown appuser:appuser /app/data

USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vitrin.Analytics.Api.dll"]
