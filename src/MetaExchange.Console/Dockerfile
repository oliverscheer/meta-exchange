# Base
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app

# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MetaExchange.Console/MetaExchange.Console.csproj", "MetaExchange.Console/"]
RUN dotnet restore "./MetaExchange.Console/MetaExchange.Console.csproj"
COPY . .
WORKDIR "/src/MetaExchange.Console"
RUN dotnet build "./MetaExchange.Console.csproj" -c $BUILD_CONFIGURATION -o /app/build

# publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MetaExchange.Console.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Run
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MetaExchange.Console.dll"]
