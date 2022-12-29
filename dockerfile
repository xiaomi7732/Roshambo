FROM mcr.microsoft.com/dotnet/aspnet:6.0 as base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY Roshambo.Backend /src/Roshambo.Backend
COPY Roshambo.Models /src/Roshambo.Models

WORKDIR /src

RUN ls
RUN dotnet build "Roshambo.Backend" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Roshambo.Backend" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Roshambo.Backend.dll"]