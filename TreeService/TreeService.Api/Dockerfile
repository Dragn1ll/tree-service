FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TreeService.Api/TreeService.Api.csproj", "TreeService.Api/"]
COPY ["TreeService.Persistence.SQLite/TreeService.Persistence.SQLite.csproj", "TreeService.Persistence.SQLite/"]
COPY ["TreeService.Domain/TreeService.Domain.csproj", "TreeService.Domain/"]
COPY ["TreeService.Contracts/TreeService.Contracts.csproj", "TreeService.Contracts/"]
RUN dotnet restore "TreeService.Api/TreeService.Api.csproj"
COPY . .
WORKDIR "/src/TreeService.Api"
RUN dotnet build "TreeService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TreeService.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TreeService.Api.dll"]
