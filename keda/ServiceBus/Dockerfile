FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EventProcessor.Api/EventProcessor.Api.csproj EventProcessor.Api/
RUN dotnet restore EventProcessor.Api/EventProcessor.Api.csproj

COPY EventProcessor.Api/ EventProcessor.Api/
RUN dotnet publish EventProcessor.Api/EventProcessor.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

USER $APP_UID

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EventProcessor.Api.dll"]
