# ==============================================================
# Vitrin Notification Service - Multi-Stage Dockerfile
# SQLite veritabanı /app/data dizininde saklanır (volume mount)
# ==============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/Vitrin.Shared.Kernel/Vitrin.Shared.Kernel.csproj",                                   "src/Shared/Vitrin.Shared.Kernel/"]
COPY ["src/Shared/Vitrin.Shared.Contracts/Vitrin.Shared.Contracts.csproj",                             "src/Shared/Vitrin.Shared.Contracts/"]
COPY ["src/Shared/Vitrin.Shared.Infrastructure/Vitrin.Shared.Infrastructure.csproj",                   "src/Shared/Vitrin.Shared.Infrastructure/"]

COPY ["src/Services/Notification/Vitrin.Notification.Domain/Vitrin.Notification.Domain.csproj",         "src/Services/Notification/Vitrin.Notification.Domain/"]
COPY ["src/Services/Notification/Vitrin.Notification.Application/Vitrin.Notification.Application.csproj", "src/Services/Notification/Vitrin.Notification.Application/"]
COPY ["src/Services/Notification/Vitrin.Notification.Infrastructure/Vitrin.Notification.Infrastructure.csproj", "src/Services/Notification/Vitrin.Notification.Infrastructure/"]
COPY ["src/Services/Notification/Vitrin.Notification.Api/Vitrin.Notification.Api.csproj",               "src/Services/Notification/Vitrin.Notification.Api/"]

RUN dotnet restore "src/Services/Notification/Vitrin.Notification.Api/Vitrin.Notification.Api.csproj"

COPY . .
RUN dotnet publish "src/Services/Notification/Vitrin.Notification.Api/Vitrin.Notification.Api.csproj" \
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
ENTRYPOINT ["dotnet", "Vitrin.Notification.Api.dll"]
