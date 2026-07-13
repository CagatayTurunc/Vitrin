# ==============================================================
# Vitrin AI Service - Multi-Stage Dockerfile
# SQLite veritabanı /app/data dizininde saklanır (volume mount)
# ==============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/Vitrin.Shared.Kernel/Vitrin.Shared.Kernel.csproj",                   "src/Shared/Vitrin.Shared.Kernel/"]
COPY ["src/Shared/Vitrin.Shared.Contracts/Vitrin.Shared.Contracts.csproj",             "src/Shared/Vitrin.Shared.Contracts/"]
COPY ["src/Shared/Vitrin.Shared.Infrastructure/Vitrin.Shared.Infrastructure.csproj",   "src/Shared/Vitrin.Shared.Infrastructure/"]

COPY ["src/Services/Ai/Vitrin.Ai.Domain/Vitrin.Ai.Domain.csproj",                      "src/Services/Ai/Vitrin.Ai.Domain/"]
COPY ["src/Services/Ai/Vitrin.Ai.Application/Vitrin.Ai.Application.csproj",            "src/Services/Ai/Vitrin.Ai.Application/"]
COPY ["src/Services/Ai/Vitrin.Ai.Infrastructure/Vitrin.Ai.Infrastructure.csproj",      "src/Services/Ai/Vitrin.Ai.Infrastructure/"]
COPY ["src/Services/Ai/Vitrin.Ai.Api/Vitrin.Ai.Api.csproj",                            "src/Services/Ai/Vitrin.Ai.Api/"]

RUN dotnet restore "src/Services/Ai/Vitrin.Ai.Api/Vitrin.Ai.Api.csproj"

COPY . .
RUN dotnet publish "src/Services/Ai/Vitrin.Ai.Api/Vitrin.Ai.Api.csproj" \
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
ENTRYPOINT ["dotnet", "Vitrin.Ai.Api.dll"]
