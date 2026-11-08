# Docs site image, built by .github/workflows/site-image.yml. Build context is the repo root.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish VetoWeb/VetoWeb/VetoWeb.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "VetoWeb.dll"]
