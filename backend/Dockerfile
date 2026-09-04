FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/Butcher.Api/Butcher.Api.csproj src/Butcher.Api/
RUN dotnet restore src/Butcher.Api/Butcher.Api.csproj

COPY src/Butcher.Api/ src/Butcher.Api/
RUN dotnet publish src/Butcher.Api/Butcher.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER app
ENTRYPOINT ["dotnet", "Butcher.Api.dll"]
