# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy csproj and restore dependencies
COPY *.sln .
COPY GeoURPWebApi/*.csproj ./GeoURPWebApi/
RUN dotnet restore

# Copy all source files and publish
COPY . .
WORKDIR /source/GeoURPWebApi
RUN dotnet publish -c Release -o /app

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Create upload directory and set broad permissions
RUN mkdir -p /app/wwwroot/uploads/board-members && chmod -R 777 /app/wwwroot/uploads

# Expose HTTP port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "GeoURPWebApi.dll"]
