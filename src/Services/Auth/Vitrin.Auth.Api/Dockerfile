# ==============================================================
# Vitrin Auth Service - Multi-Stage Dockerfile
# ==============================================================

# Aşama 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Shared projeler - layer caching için önce csproj dosyaları kopyalanır
COPY ["src/Shared/Vitrin.Shared.Kernel/Vitrin.Shared.Kernel.csproj",                       "src/Shared/Vitrin.Shared.Kernel/"]
COPY ["src/Shared/Vitrin.Shared.Contracts/Vitrin.Shared.Contracts.csproj",                 "src/Shared/Vitrin.Shared.Contracts/"]
COPY ["src/Shared/Vitrin.Shared.Infrastructure/Vitrin.Shared.Infrastructure.csproj",       "src/Shared/Vitrin.Shared.Infrastructure/"]

# Auth servisi csproj dosyaları
COPY ["src/Services/Auth/Vitrin.Auth.Domain/Vitrin.Auth.Domain.csproj",                    "src/Services/Auth/Vitrin.Auth.Domain/"]
COPY ["src/Services/Auth/Vitrin.Auth.Application/Vitrin.Auth.Application.csproj",          "src/Services/Auth/Vitrin.Auth.Application/"]
COPY ["src/Services/Auth/Vitrin.Auth.Infrastructure/Vitrin.Auth.Infrastructure.csproj",    "src/Services/Auth/Vitrin.Auth.Infrastructure/"]
COPY ["src/Services/Auth/Vitrin.Auth.Api/Vitrin.Auth.Api.csproj",                          "src/Services/Auth/Vitrin.Auth.Api/"]

# Restore - sadece bağımlılıklar değiştiğinde tekrar çalışır
RUN dotnet restore "src/Services/Auth/Vitrin.Auth.Api/Vitrin.Auth.Api.csproj"

# Kaynak kodu kopyala ve yayımla
COPY . .
RUN dotnet publish "src/Services/Auth/Vitrin.Auth.Api/Vitrin.Auth.Api.csproj" \
    -c Release -o /app/publish --no-restore

# ==============================================================
# Aşama 2: Runtime (küçük imaj)
# ==============================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

# curl kurulumu (healthcheck vb. için)
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Güvenlik: non-root kullanıcı
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vitrin.Auth.Api.dll"]
