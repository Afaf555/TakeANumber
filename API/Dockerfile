FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY API.csproj ./
RUN dotnet restore API.csproj

COPY . ./
RUN dotnet publish API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=Europe/Skopje

COPY --from=build /app/publish ./

EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
