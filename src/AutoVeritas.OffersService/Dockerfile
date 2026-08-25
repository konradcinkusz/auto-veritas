# Build context is the repository root (fly.toml and docker-compose declare it).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

# Restore layers survive source changes: project files first, source afterwards.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/AutoVeritas.ServiceDefaults/AutoVeritas.ServiceDefaults.csproj src/AutoVeritas.ServiceDefaults/
COPY src/AutoVeritas.OffersService.Data/AutoVeritas.OffersService.Data.csproj src/AutoVeritas.OffersService.Data/
COPY src/AutoVeritas.OffersService.Migrations.PostgreSQL/AutoVeritas.OffersService.Migrations.PostgreSQL.csproj src/AutoVeritas.OffersService.Migrations.PostgreSQL/
COPY src/AutoVeritas.OffersService/AutoVeritas.OffersService.csproj src/AutoVeritas.OffersService/
RUN dotnet restore src/AutoVeritas.OffersService/AutoVeritas.OffersService.csproj -a "$TARGETARCH"

COPY src/ src/
WORKDIR /src/src/AutoVeritas.OffersService
RUN dotnet publish -c Release -a "$TARGETARCH" -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
LABEL org.opencontainers.image.source=https://github.com/konradcinkusz/auto-veritas
ENTRYPOINT ["dotnet", "AutoVeritas.OffersService.dll"]
