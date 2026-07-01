# Build context is the repository root (see docker-compose.yml) because this
# project references sibling projects under src/ via ProjectReference.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Steward.Api/Steward.Api.csproj src/Steward.Api/
COPY src/Steward.Application/Steward.Application.csproj src/Steward.Application/
COPY src/Steward.Domain/Steward.Domain.csproj src/Steward.Domain/
COPY src/Steward.Infrastructure/Steward.Infrastructure.csproj src/Steward.Infrastructure/
RUN dotnet restore src/Steward.Api/Steward.Api.csproj

COPY src/Steward.Api/ src/Steward.Api/
COPY src/Steward.Application/ src/Steward.Application/
COPY src/Steward.Domain/ src/Steward.Domain/
COPY src/Steward.Infrastructure/ src/Steward.Infrastructure/
RUN dotnet publish src/Steward.Api/Steward.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Steward.Api.dll"]
