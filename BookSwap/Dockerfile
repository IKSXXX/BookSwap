FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BookSwap.Db/BookSwap.Db.csproj BookSwap.Db/
COPY BookSwap/BookSwap.Web.csproj BookSwap/
RUN dotnet restore BookSwap/BookSwap.Web.csproj

COPY BookSwap.Db/ BookSwap.Db/
COPY BookSwap/ BookSwap/
RUN dotnet publish BookSwap/BookSwap.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BookSwap.Web.dll"]
