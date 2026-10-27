# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY src/AstraFleetWars.Api/*.csproj ./src/AstraFleetWars.Api/
RUN dotnet restore src/AstraFleetWars.Api/AstraFleetWars.Api.csproj

# Copy full source and publish
COPY src/AstraFleetWars.Api/ ./src/AstraFleetWars.Api/
WORKDIR /app/src/AstraFleetWars.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "AstraFleetWars.Api.dll"]
