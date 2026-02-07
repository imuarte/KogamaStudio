FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build TranslateAPI.AppHost/TranslateAPI.AppHost.csproj -c Release

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /src/TranslateAPI.AppHost/bin/Release/net8.0/ .
EXPOSE 5000
CMD ["dotnet", "TranslateAPI.AppHost.dll"]
