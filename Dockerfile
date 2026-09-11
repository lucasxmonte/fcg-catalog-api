FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Feed local com FCG.Contracts
COPY packages/ packages/
COPY NuGet.Config .

COPY src/FCG.CatalogAPI.Domain/FCG.CatalogAPI.Domain.csproj            src/FCG.CatalogAPI.Domain/
COPY src/FCG.CatalogAPI.Application/FCG.CatalogAPI.Application.csproj  src/FCG.CatalogAPI.Application/
COPY src/FCG.CatalogAPI.Infrastructure/FCG.CatalogAPI.Infrastructure.csproj src/FCG.CatalogAPI.Infrastructure/
COPY src/FCG.CatalogAPI.API/FCG.CatalogAPI.API.csproj                   src/FCG.CatalogAPI.API/
RUN dotnet restore src/FCG.CatalogAPI.API/FCG.CatalogAPI.API.csproj \
    --configfile ./NuGet.Config

COPY src/ src/
WORKDIR /src/src/FCG.CatalogAPI.API
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_ENVIRONMENT=Docker
ENV ASPNETCORE_URLS=http://+:8080
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "FCG.CatalogAPI.API.dll"]
