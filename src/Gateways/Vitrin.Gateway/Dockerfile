# ==============================================================
# Vitrin API Gateway (YARP) - Multi-Stage Dockerfile
# ==============================================================

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Gateways/Vitrin.Gateway/Vitrin.Gateway.csproj", "src/Gateways/Vitrin.Gateway/"]

RUN dotnet restore "src/Gateways/Vitrin.Gateway/Vitrin.Gateway.csproj"

COPY . .
RUN dotnet publish "src/Gateways/Vitrin.Gateway/Vitrin.Gateway.csproj" \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Vitrin.Gateway.dll"]
