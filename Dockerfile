# Use the official .NET 8 runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Explicit Render/Kestrel port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Use the SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ProductionTracker.csproj", "."]
RUN dotnet restore "./ProductionTracker.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ProductionTracker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProductionTracker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductionTracker.dll"]
