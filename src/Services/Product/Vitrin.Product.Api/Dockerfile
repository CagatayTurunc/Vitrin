# ==============================================================
# Vitrin Product Service - Multi-Stage Dockerfile
# ==============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/Vitrin.Shared.Kernel/Vitrin.Shared.Kernel.csproj",                       "src/Shared/Vitrin.Shared.Kernel/"]
COPY ["src/Shared/Vitrin.Shared.Contracts/Vitrin.Shared.Contracts.csproj",                 "src/Shared/Vitrin.Shared.Contracts/"]
COPY ["src/Shared/Vitrin.Shared.Infrastructure/Vitrin.Shared.Infrastructure.csproj",       "src/Shared/Vitrin.Shared.Infrastructure/"]

COPY ["src/Services/Product/Vitrin.Product.Domain/Vitrin.Product.Domain.csproj",           "src/Services/Product/Vitrin.Product.Domain/"]
COPY ["src/Services/Product/Vitrin.Product.Application/Vitrin.Product.Application.csproj", "src/Services/Product/Vitrin.Product.Application/"]
COPY ["src/Services/Product/Vitrin.Product.Infrastructure/Vitrin.Product.Infrastructure.csproj", "src/Services/Product/Vitrin.Product.Infrastructure/"]
COPY ["src/Services/Product/Vitrin.Product.Api/Vitrin.Product.Api.csproj",                 "src/Services/Product/Vitrin.Product.Api/"]

RUN dotnet restore "src/Services/Product/Vitrin.Product.Api/Vitrin.Product.Api.csproj"

COPY . .
RUN dotnet publish "src/Services/Product/Vitrin.Product.Api/Vitrin.Product.Api.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vitrin.Product.Api.dll"]
