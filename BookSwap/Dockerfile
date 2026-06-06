FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BookExchange.Db/BookExchange.Db.csproj BookExchange.Db/
COPY BookSwap/BookExchange.Web.csproj BookSwap/
RUN dotnet restore BookSwap/BookExchange.Web.csproj

COPY BookExchange.Db/ BookExchange.Db/
COPY BookSwap/ BookSwap/
RUN dotnet publish BookSwap/BookExchange.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookExchange.Web.dll"]
