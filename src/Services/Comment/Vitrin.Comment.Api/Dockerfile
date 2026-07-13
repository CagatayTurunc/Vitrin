# ==============================================================
# Vitrin Comment Service - Multi-Stage Dockerfile
# ==============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/Vitrin.Shared.Kernel/Vitrin.Shared.Kernel.csproj",                       "src/Shared/Vitrin.Shared.Kernel/"]
COPY ["src/Shared/Vitrin.Shared.Contracts/Vitrin.Shared.Contracts.csproj",                 "src/Shared/Vitrin.Shared.Contracts/"]
COPY ["src/Shared/Vitrin.Shared.Infrastructure/Vitrin.Shared.Infrastructure.csproj",       "src/Shared/Vitrin.Shared.Infrastructure/"]

COPY ["src/Services/Comment/Vitrin.Comment.Domain/Vitrin.Comment.Domain.csproj",           "src/Services/Comment/Vitrin.Comment.Domain/"]
COPY ["src/Services/Comment/Vitrin.Comment.Application/Vitrin.Comment.Application.csproj", "src/Services/Comment/Vitrin.Comment.Application/"]
COPY ["src/Services/Comment/Vitrin.Comment.Infrastructure/Vitrin.Comment.Infrastructure.csproj", "src/Services/Comment/Vitrin.Comment.Infrastructure/"]
COPY ["src/Services/Comment/Vitrin.Comment.Api/Vitrin.Comment.Api.csproj",                 "src/Services/Comment/Vitrin.Comment.Api/"]

RUN dotnet restore "src/Services/Comment/Vitrin.Comment.Api/Vitrin.Comment.Api.csproj"

COPY . .
RUN dotnet publish "src/Services/Comment/Vitrin.Comment.Api/Vitrin.Comment.Api.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vitrin.Comment.Api.dll"]
