# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Copy csproj and restore dependencies
COPY src/AstraFleetWars.Api/AstraFleetWars.Api.csproj src/AstraFleetWars.Api/
RUN dotnet restore src/AstraFleetWars.Api/AstraFleetWars.Api.csproj

# Copy remaining source code and publish
COPY src/AstraFleetWars.Api/ src/AstraFleetWars.Api/
WORKDIR /src/src/AstraFleetWars.Api
RUN dotnet publish AstraFleetWars.Api.csproj -c Release -o /app/publish -p:UseSharedCompilation=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080;http://+:5000
EXPOSE 8080
EXPOSE 5000

ENTRYPOINT ["dotnet", "AstraFleetWars.Api.dll"]
